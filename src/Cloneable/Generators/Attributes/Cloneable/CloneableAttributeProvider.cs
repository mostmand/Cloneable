using System.Text;
using Cloneable.Generators.Attributes.Abstraction;
using Microsoft.CodeAnalysis.Text;

namespace Cloneable.Generators.Attributes.Cloneable;

internal class CloneableAttributeProvider : IAttributeProvider
{
    private const string Text = @"using System;

namespace " + CloneableConstants.RootNamespace + @"
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
    public sealed class " + CloneableAttributeConstants.CloneableAttributeString + @" : Attribute
    {
        public " + CloneableAttributeConstants.CloneableAttributeString + @"()
        {
        }

        public bool " + CloneableAttributeConstants.ExplicitDeclarationKeyString + @" { get; set; }
    }
}
";
    
    public AttributeInfo AttributeInfo { get; } = new()
    {
        Name = CloneableAttributeConstants.CloneableAttributeString,
        SourceText = SourceText.From(Text, Encoding.UTF8)
    };
}