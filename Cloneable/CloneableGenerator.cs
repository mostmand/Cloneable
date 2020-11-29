using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
        private const string cloneableAttributeText = @"using System;

namespace Cloneable
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
    public sealed class CloneableAttribute : Attribute
    {
        private bool explicitDeclaration;
        public CloneableAttribute()
        {
        }

        public bool ExplicitDeclaration { get => explicitDeclaration; set => explicitDeclaration = value; }
    }
}
";
        private const string clonePropertyAttributeText = @"using System;

namespace Cloneable
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class CloneAttribute : Attribute
    {
        private bool preventDeepCopy;
        public CloneAttribute()
        {
        }
        public bool PreventDeepCopy { get => preventDeepCopy; set => preventDeepCopy = value; }
    }
}
";
        private const string ignoreClonePropertyAttributeText = @"using System;

namespace Cloneable
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class IgnoreCloneAttribute : Attribute
    {
        public IgnoreCloneAttribute()
        {
        }
    }
}
";

        private INamedTypeSymbol cloneableAttribute;
        private INamedTypeSymbol ignoreCloneAttribute;
        private INamedTypeSymbol cloneAttribute;

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

            var options = (context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;
            
            var compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(cloneableAttributeText, Encoding.UTF8), options)).
                AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(clonePropertyAttributeText, Encoding.UTF8), options)).
                AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(ignoreClonePropertyAttributeText, Encoding.UTF8), options));

            cloneableAttribute = compilation.GetTypeByMetadataName("Cloneable.CloneableAttribute")!;
            cloneAttribute = compilation.GetTypeByMetadataName("Cloneable.CloneAttribute")!;
            ignoreCloneAttribute = compilation.GetTypeByMetadataName("Cloneable.IgnoreCloneAttribute")!;

            var classSymbols = GetClassSymbols(compilation, cloneableAttribute, receiver);
            foreach (var classSymbol in classSymbols)
            {
                var attribute = classSymbol.GetAttributes().First(x => x.AttributeClass.Equals(cloneableAttribute));
                var isExplicit = (bool?)attribute.NamedArguments.FirstOrDefault(e => e.Key.Equals("ExplicitDeclaration")).Value.Value ?? false;
                context.AddSource($"{classSymbol.Name}_cloneable.cs", SourceText.From(CreateCloneableCode(classSymbol, isExplicit), Encoding.UTF8));
            }
        }

        private string CreateCloneableCode(INamedTypeSymbol classSymbol, bool isExplicit)
        {
            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
            var fieldAssignmentsCode = GenerateFieldAssignmentsCode(classSymbol, isExplicit, false);
            var fieldAssignmentsCodeSafe = fieldAssignmentsCode.Select(x =>
            {
                if (x.EndsWith("?.Clone"))
                    return x + "Safe(referenceChain)";
                return x;
            });
            fieldAssignmentsCode = fieldAssignmentsCode.Select(x =>
            {
                if (x.EndsWith("?.Clone"))
                    return x + "()";
                return x;
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
{string.Join($",{Environment.NewLine}", fieldAssignmentsCode)}
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

        private IEnumerable<string> GenerateFieldAssignmentsCode(INamedTypeSymbol classSymbol, bool isExplicit, bool safe)
        {
            var fieldNames = GetCloneableProperties(classSymbol, isExplicit);
            var fieldAssignments = fieldNames.Select(x =>
                {
                    bool isCloneable = x.Type != classSymbol && 
                                       x.Type.GetAttributes().Any(a => a.AttributeClass == cloneableAttribute && 
                                                                       !x.GetAttributes().Any(i => (bool?)i.NamedArguments.FirstOrDefault(e => e.Key.Equals("PreventDeepCopy")).Value.Value ?? false));
                    return (item: x, isCloneable);
                }).
                OrderBy(x => x.isCloneable).
                Select(x =>
                {
                    var name = x.item.Name;
                    if (x.isCloneable)
                    {
                        return $@"                {name} = this.{name}?.Clone";
                    }
                    return $@"                {name} = this.{name}";
                });
            return fieldAssignments;
        }

        private string GetAccessModifier(INamedTypeSymbol classSymbol)
        {
            return classSymbol.DeclaredAccessibility.ToString().ToLowerInvariant();
        }

        private IEnumerable<IPropertySymbol> GetCloneableProperties(ITypeSymbol classSymbol, bool isExplicit)
        {
            var enumerable = classSymbol.GetMembers().OfType<IPropertySymbol>()
                .Where(x => x.SetMethod is not null &&
                            x.CanBeReferencedByName);
            if (isExplicit)
            {
                enumerable = enumerable.Where(x =>
                    x.GetAttributes().Any(a => a.AttributeClass == cloneAttribute));
            }
            else
            {
                enumerable = enumerable.Where(x =>
                    !x.GetAttributes().Any(a => a.AttributeClass.Equals(ignoreCloneAttribute)));
            }

            return enumerable;
        }

        private static List<INamedTypeSymbol> GetClassSymbols(Compilation compilation, INamedTypeSymbol cloneableAttributeSymbol, SyntaxReceiver receiver)
        {
            var classSymbols = new List<INamedTypeSymbol>();
            foreach (var clazz in receiver.CandidateClasses)
            {
                INamedTypeSymbol classSymbol = GetClassSymbol(compilation, clazz);
                if (HasAttribute(classSymbol, cloneableAttributeSymbol))
                {
                    classSymbols.Add(classSymbol);
                }
            }

            return classSymbols;
        }

        private static bool HasAttribute(INamedTypeSymbol classSymbol, INamedTypeSymbol attributeSymbol)
        {
            return classSymbol.GetAttributes().Any(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));
        }

        private static INamedTypeSymbol GetClassSymbol(Compilation compilation, Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax clazz)
        {
            var model = compilation.GetSemanticModel(clazz.SyntaxTree);
            var classSymbol = model.GetDeclaredSymbol(clazz)!;
            return classSymbol;
        }

        private static void InjectCloneableAttributes(GeneratorExecutionContext context)
        {
            context.AddSource("CloneableAttribute", SourceText.From(cloneableAttributeText, Encoding.UTF8));
            context.AddSource("ClonePropertyAttribute", SourceText.From(clonePropertyAttributeText, Encoding.UTF8));
            context.AddSource("IgnoreClonePropertyAttribute", SourceText.From(ignoreClonePropertyAttributeText, Encoding.UTF8));
        }
    }
}