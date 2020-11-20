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
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    sealed class CloneableAttribute : Attribute
    {
        public CloneableAttribute()
        {
        }
    }
}
";

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            InjectCloneableAttribute(context);
            GenerateCloneMethods(context);
        }

        private void GenerateCloneMethods(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not SyntaxReceiver receiver)
                return;

            var classSymbols = GetClassSymbols(context, receiver);
            foreach (var classSymbol in classSymbols)
            {
                context.AddSource($"{classSymbol.Name}_cloneable.cs", SourceText.From(CreateCloneableCode(classSymbol), Encoding.UTF8));
            }
        }

        private string CreateCloneableCode(INamedTypeSymbol classSymbol)
        {
            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
            string fieldAssignmentsCode = GenerateFieldAssignmentsCode(classSymbol);

            return $@"namespace {namespaceName}
{{
    {GetAccessModifier(classSymbol)} partial class {classSymbol.Name}
    {{
        public {classSymbol.Name} Clone()
        {{
            return new {classSymbol.Name}
            {{
                {fieldAssignmentsCode}
            }};
        }}
    }}
}}";
        }

        private static string GenerateFieldAssignmentsCode(INamedTypeSymbol classSymbol)
        {
            var fieldNames = GetFieldsAndProperties(classSymbol);
            var fieldAssignments = fieldNames.Select(x => $@"                {x} = this.{x}");
            var fieldAssignmentsCode = string.Join($",{Environment.NewLine}", fieldAssignments);
            return fieldAssignmentsCode;
        }

        private static string GetAccessModifier(INamedTypeSymbol classSymbol)
        {
            return classSymbol.DeclaredAccessibility.ToString().ToLowerInvariant();
        }

        private static IEnumerable<string> GetFieldsAndProperties(INamedTypeSymbol classSymbol)
        {
            IEnumerable<string> properties = GetProperties(classSymbol);
            IEnumerable<string> fields = GetFields(classSymbol);
            return properties.Concat(fields);
        }

        private static IEnumerable<string> GetFields(INamedTypeSymbol classSymbol)
        {
            return classSymbol.GetMembers().OfType<IFieldSymbol>()
               .Where(x => x.CanBeReferencedByName)
               .Select(fieldSymbol => $"{fieldSymbol.Name}")
               .ToList();
        }

        private static IEnumerable<string> GetProperties(INamedTypeSymbol classSymbol)
        {
            return classSymbol.GetMembers().OfType<IPropertySymbol>()
                .Where(x => x.SetMethod is not null)
                .Where(x => x.CanBeReferencedByName)
                .Select(propertySymbol => $"{propertySymbol.Name}")
                .ToList();
        }

        private static List<INamedTypeSymbol> GetClassSymbols(GeneratorExecutionContext context, SyntaxReceiver receiver)
        {
            var options = (context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;
            var compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(cloneableAttributeText, Encoding.UTF8), options));

            var cloneableAttributeSymbol = compilation.GetTypeByMetadataName("Cloneable.CloneableAttribute")!;

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

        private static void InjectCloneableAttribute(GeneratorExecutionContext context)
        {
            context.AddSource("CloneableAttribute", SourceText.From(cloneableAttributeText, Encoding.UTF8));
        }
    }
}