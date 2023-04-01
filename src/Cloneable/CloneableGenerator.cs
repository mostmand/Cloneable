using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cloneable.Generators.Attributes;
using Cloneable.Generators.Attributes.Abstraction;
using Cloneable.Generators.Attributes.Clone;
using Cloneable.Generators.Attributes.Cloneable;
using Cloneable.Generators.Attributes.IgnoreClone;

namespace Cloneable;

[Generator]
public class CloneableGenerator : ISourceGenerator
{
    private INamedTypeSymbol? _cloneableAttribute;
    private INamedTypeSymbol? _ignoreCloneAttribute;
    private INamedTypeSymbol? _cloneAttribute;
    private static readonly IAttributeFinder AttributeFinder = new AttributeFinder();

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        // System.Diagnostics.Debugger.Launch();
        var attributes = AttributeFinder.FindAttributes();
        InjectAttributesCode(context, attributes);
        GenerateCloneMethods(context, attributes);
    }

    private void GenerateCloneMethods(GeneratorExecutionContext context, IEnumerable<AttributeInfo> attributeInfos)
    {
        if (context.SyntaxReceiver is not SyntaxReceiver receiver)
            return;

        var compilation = AddAttributesSyntaxTrees(context, attributeInfos);

        InitAttributes(compilation);

        var cloneableCodes = GenerateCloneableCodes(compilation, receiver);

        AddGeneratedSourcesToContext(context, cloneableCodes);
    }

    private static void AddGeneratedSourcesToContext(GeneratorExecutionContext context, List<GeneratedCode> cloneableCodes)
    {
        foreach (var cloneableCode in cloneableCodes)
        {
            context.AddSource(cloneableCode.FileName, SourceText.From(cloneableCode.Code, Encoding.UTF8));
        }
    }

    private List<GeneratedCode> GenerateCloneableCodes(Compilation compilation, SyntaxReceiver receiver)
    {
        var classSymbols = GetClassSymbols(compilation, receiver);
        var cloneableCodes = new List<GeneratedCode>();
        foreach (var classSymbol in classSymbols)
        {
            if (!classSymbol.TryGetAttribute(_cloneableAttribute!, out var attributes))
                continue;

            var attribute = attributes.Single();
            var isExplicit = IsExplicit(attribute);

            var fileName = $"{classSymbol.Name}_cloneable.cs";
            var code = CreateCloneableCode(classSymbol, isExplicit);
            var cloneableCode = new GeneratedCode(fileName, code);
            cloneableCodes.Add(cloneableCode);
        }

        return cloneableCodes;
    }

    private static bool IsExplicit(AttributeData attribute)
    {
        return (bool?)attribute.NamedArguments
            .FirstOrDefault(e => e.Key.Equals(CloneableAttributeConstants.ExplicitDeclarationKeyString))
            .Value
            .Value ?? false;
    }

    private void InitAttributes(Compilation compilation)
    {
        _cloneableAttribute = compilation.GetTypeByMetadataName($"{CloneableConstants.RootNamespace}.{CloneableAttributeConstants.CloneableAttributeString}")!;
        _cloneAttribute = compilation.GetTypeByMetadataName($"{CloneableConstants.RootNamespace}.{CloneAttributeConstants.CloneAttributeString}")!;
        _ignoreCloneAttribute = compilation.GetTypeByMetadataName($"{CloneableConstants.RootNamespace}.{IgnoreCloneAttributeConstants.IgnoreCloneAttributeString}")!;
    }

    private static Compilation AddAttributesSyntaxTrees(GeneratorExecutionContext context, IEnumerable<AttributeInfo> attributes)
    {
        var options = context.Compilation.SyntaxTrees.First().Options as CSharpParseOptions;

        var compilation = context.Compilation;
        foreach (var attribute in attributes)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(attribute.SourceText, options);
            compilation = compilation.AddSyntaxTrees(syntaxTree);
        }
        
        return compilation;
    }

    private string CreateCloneableCode(INamedTypeSymbol classSymbol, bool isExplicit)
    {
        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        var fieldAssignmentsCode = GenerateFieldAssignmentsCode(classSymbol, isExplicit).ToList();
        var fieldAssignmentsCodeSafe = fieldAssignmentsCode.Select(x =>
        {
            if (x.isCloneable)
                return x.line + "Safe(referenceChain)";
            return x.line;
        });
        var fieldAssignmentsCodeFast = fieldAssignmentsCode.Select(x =>
        {
            if (x.isCloneable)
                return x.line + "()";
            return x.line;
        });

        return $@"using System.Collections.Generic;

namespace {namespaceName}
{{
    {GetAccessModifier(classSymbol)} partial class {classSymbol.Name}
    {{
        /// <summary>
        /// Creates a copy of {classSymbol.Name} with NO circular reference checking. This method should be used if performance matters.
        /// 
        /// <exception cref=""StackOverflowException"">Will occur on any object that has circular references in the hierarchy.</exception>
        /// </summary>
        public {classSymbol.Name} Clone()
        {{
            return new {classSymbol.Name}
            {{
{string.Join($",{Environment.NewLine}", fieldAssignmentsCodeFast)}
            }};
        }}

        /// <summary>
        /// Creates a copy of {classSymbol.Name} with circular reference checking. If a circular reference was detected, only a reference of the leaf object is passed instead of cloning it.
        /// </summary>
        /// <param name=""referenceChain"">Should only be provided if specific objects should not be cloned but passed by reference instead.</param>
        public {classSymbol.Name} CloneSafe(Stack<object> referenceChain = null)
        {{
            if(referenceChain?.Contains(this) == true) 
                return this;
            referenceChain ??= new Stack<object>();
            referenceChain.Push(this);
            var result = new {classSymbol.Name}
            {{
{string.Join($",{Environment.NewLine}", fieldAssignmentsCodeSafe)}
            }};
            referenceChain.Pop();
            return result;
        }}
    }}
}}";
    }

    private IEnumerable<(string line, bool isCloneable)> GenerateFieldAssignmentsCode(INamedTypeSymbol classSymbol, bool isExplicit)
    {
        var fieldNames = GetCloneableProperties(classSymbol, isExplicit);

        var fieldAssignments = fieldNames.Select(field => IsFieldCloneable(field, classSymbol))
            .OrderBy(x => x.isCloneable)
            .Select(x => (GenerateAssignmentCode(x.item.Name, x.isCloneable), x.isCloneable));
        return fieldAssignments;
    }

    private string GenerateAssignmentCode(string name, bool isCloneable)
    {
        if (isCloneable)
        {
            return $@"                {name} = this.{name}?.Clone";
        }

        return $@"                {name} = this.{name}";
    }

    private (IPropertySymbol item, bool isCloneable) IsFieldCloneable(IPropertySymbol x, INamedTypeSymbol classSymbol)
    {
        if (SymbolEqualityComparer.Default.Equals(x.Type, classSymbol))
        {
            return (x, false);
        }

        if (!x.Type.TryGetAttribute(_cloneableAttribute!, out var attributes))
        {
            return (x, false);
        }

        var preventDeepCopy = (bool?)attributes.Single().NamedArguments.FirstOrDefault(e => e.Key.Equals(CloneAttributeConstants.PreventDeepCopyKeyString)).Value.Value ?? false;
        return (item: x, !preventDeepCopy);
    }

    private string GetAccessModifier(INamedTypeSymbol classSymbol)
    {
        return classSymbol.DeclaredAccessibility.ToString().ToLowerInvariant();
    }

    private IEnumerable<IPropertySymbol> GetCloneableProperties(ITypeSymbol classSymbol, bool isExplicit)
    {
        var targetSymbolMembers = classSymbol.GetMembers().OfType<IPropertySymbol>()
            .Where(x => x.SetMethod is not null &&
                        x.CanBeReferencedByName);
        if (isExplicit)
        {
            return targetSymbolMembers.Where(x => x.HasAttribute(_cloneAttribute!));
        }
        else
        {
            return targetSymbolMembers.Where(x => !x.HasAttribute(_ignoreCloneAttribute!));
        }
    }

    private static IEnumerable<INamedTypeSymbol> GetClassSymbols(Compilation compilation, SyntaxReceiver receiver)
    {
        return receiver.CandidateClasses.Select(clazz => GetClassSymbol(compilation, clazz));
    }

    private static INamedTypeSymbol GetClassSymbol(Compilation compilation, ClassDeclarationSyntax clazz)
    {
        var model = compilation.GetSemanticModel(clazz.SyntaxTree);
        var classSymbol = model.GetDeclaredSymbol(clazz)!;
        return classSymbol;
    }

    private static void InjectAttributesCode(GeneratorExecutionContext context, IEnumerable<AttributeInfo> attributes)
    {
        foreach (var attribute in attributes)
        {
            context.AddSource(attribute.Name, attribute.SourceText);    
        }
    }
}