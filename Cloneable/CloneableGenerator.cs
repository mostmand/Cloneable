using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

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

            var source = new StringBuilder($@"namespace {namespaceName}
{{
    public partial class {classSymbol.Name}
    {{
        public {classSymbol.Name} Clone()
        {{
            return new {classSymbol.Name}
            {{
");
            var fieldNames = GetFieldNames(classSymbol);
            var fieldAssignments = fieldNames.Select(x => $@"                {x} = this.{x}");
            source.Append(string.Join(@",
", fieldAssignments));

            source.Append($@"
            }};
        }}
    }}
}}");
            return source.ToString();
        }

        private static IEnumerable<string> GetFieldNames(INamedTypeSymbol classSymbol)
        {
            var fieldNames = new List<string>();
            foreach (var propertySymbol in classSymbol.GetMembers().OfType<IPropertySymbol>().Where(x => x.SetMethod is not null).Where(x => x.CanBeReferencedByName))
            {
                fieldNames.Add($"{propertySymbol.Name}");
            }

            foreach (var fieldSymbol in classSymbol.GetMembers().OfType<IFieldSymbol>().Where(x => x.CanBeReferencedByName))
            {
                fieldNames.Add($"{fieldSymbol.Name}");
            }
            return fieldNames;
        }

        private static List<INamedTypeSymbol> GetClassSymbols(GeneratorExecutionContext context, SyntaxReceiver receiver)
        {
            var options = (context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;
            var compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(cloneableAttributeText, Encoding.UTF8), options));

            var attributeSymbol = compilation.GetTypeByMetadataName("Cloneable.CloneableAttribute")!;

            var classSymbols = new List<INamedTypeSymbol>();
            foreach (var clazz in receiver.CandidateClasses)
            {
                var model = compilation.GetSemanticModel(clazz.SyntaxTree);
                var classSymbol = model.GetDeclaredSymbol(clazz)!;
                if (classSymbol.GetAttributes().Any(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default)))
                {
                    classSymbols.Add(classSymbol);
                }
            }

            return classSymbols;
        }

        private static void InjectCloneableAttribute(GeneratorExecutionContext context)
        {
            context.AddSource("CloneableAttribute", SourceText.From(cloneableAttributeText, Encoding.UTF8));
        }
    }
}