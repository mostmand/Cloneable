using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloneable
{
    [Generator]
    public class CloneableGenerator : ISourceGenerator
    {
        private const string PreventDeepCopyKeyString = "PreventDeepCopy";
        private const string ExplicitDeclarationKeyString = "ExplicitDeclaration";

        private const string CloneableNamespace = "Cloneable";
        private const string CloneableAttributeString = "CloneableAttribute";
        private const string CloneAttributeString = "CloneAttribute";
        private const string IgnoreCloneAttributeString = "IgnoreCloneAttribute";

        private const string cloneableAttributeText = @"using System;

namespace " + CloneableNamespace + @"
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
    public sealed class " + CloneableAttributeString + @" : Attribute
    {
        public " + CloneableAttributeString + @"()
        {
        }

        public bool " + ExplicitDeclarationKeyString + @" { get; set; }
    }
}
";

        private const string clonePropertyAttributeText = @"using System;

namespace " + CloneableNamespace + @"
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class " + CloneAttributeString + @" : Attribute
    {
        public " + CloneAttributeString + @"()
        {
        }

        public bool " + PreventDeepCopyKeyString + @" { get; set; }
    }
}
";

        private const string ignoreClonePropertyAttributeText = @"using System;

namespace " + CloneableNamespace + @"
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class " + IgnoreCloneAttributeString + @" : Attribute
    {
        public " + IgnoreCloneAttributeString + @"()
        {
        }
    }
}
";

        private INamedTypeSymbol? cloneableAttribute;
        private INamedTypeSymbol? ignoreCloneAttribute;
        private INamedTypeSymbol? cloneAttribute;

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            InjectCloneableAttributes(context);
            GenerateCloneMethods(context);
        }

        private void GenerateCloneMethods(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not SyntaxReceiver receiver)
                return;

            Compilation compilation = GetCompilation(context);

            InitAttributes(compilation);

            var classSymbols = GetClassSymbols(compilation, receiver);
            foreach (var classSymbol in classSymbols)
            {
                if (!classSymbol.TryGetAttribute(cloneableAttribute!, out var attributes))
                    continue;

                var attribute = attributes.Single();
                var isExplicit = (bool?)attribute.NamedArguments.FirstOrDefault(e => e.Key.Equals(ExplicitDeclarationKeyString)).Value.Value ?? false;
                context.AddSource($"{classSymbol.Name}_cloneable.cs", SourceText.From(CreateCloneableCode(classSymbol, isExplicit), Encoding.UTF8));
            }
        }

        private void InitAttributes(Compilation compilation)
        {
            cloneableAttribute = compilation.GetTypeByMetadataName($"{CloneableNamespace}.{CloneableAttributeString}")!;
            cloneAttribute = compilation.GetTypeByMetadataName($"{CloneableNamespace}.{CloneAttributeString}")!;
            ignoreCloneAttribute = compilation.GetTypeByMetadataName($"{CloneableNamespace}.{IgnoreCloneAttributeString}")!;
        }

        private static Compilation GetCompilation(GeneratorExecutionContext context)
        {
            var options = context.Compilation.SyntaxTrees.First().Options as CSharpParseOptions;

            var compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(cloneableAttributeText, Encoding.UTF8), options)).
                AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(clonePropertyAttributeText, Encoding.UTF8), options)).
                AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(ignoreClonePropertyAttributeText, Encoding.UTF8), options));
            return compilation;
        }

        private string CreateCloneableCode(INamedTypeSymbol classSymbol, bool isExplicit)
        {
            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
            var fieldAssignmentsCode = GenerateFieldAssignmentsCode(classSymbol, isExplicit);
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

        private IEnumerable<(string line, bool isCloneable)> GenerateFieldAssignmentsCode(INamedTypeSymbol classSymbol, bool isExplicit )
        {
            var fieldNames = GetCloneableProperties(classSymbol, isExplicit);

            var fieldAssignments = fieldNames.Select(field => IsFieldCloneable(field, classSymbol)).
                OrderBy(x => x.isCloneable).
                Select(x => (GenerateAssignmentCode(x.item.Name, x.isCloneable), x.isCloneable));
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

            if (!x.Type.TryGetAttribute(cloneableAttribute!, out var attributes))
            {
                return (x, false);
            }

            var preventDeepCopy = (bool?)attributes.Single().NamedArguments.FirstOrDefault(e => e.Key.Equals(PreventDeepCopyKeyString)).Value.Value ?? false;
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
                return targetSymbolMembers.Where(x => x.HasAttribute(cloneAttribute!));
            }
            else
            {
                return targetSymbolMembers.Where(x => !x.HasAttribute(ignoreCloneAttribute!));
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

        private static void InjectCloneableAttributes(GeneratorExecutionContext context)
        {
            context.AddSource(CloneableAttributeString, SourceText.From(cloneableAttributeText, Encoding.UTF8));
            context.AddSource(CloneAttributeString, SourceText.From(clonePropertyAttributeText, Encoding.UTF8));
            context.AddSource(IgnoreCloneAttributeString, SourceText.From(ignoreClonePropertyAttributeText, Encoding.UTF8));
        }
    }
}