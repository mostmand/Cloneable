using System.Text;
using Cloneable.Generators.Attributes.Abstraction;
using Microsoft.CodeAnalysis.Text;

namespace Cloneable.Generators.Attributes.Clone;

internal class CloneAttributeProvider : IAttributeProvider
{
    private const string Text = @"using System;

namespace " + CloneableConstants.RootNamespace + @"
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class " + CloneAttributeConstants.CloneAttributeString + @" : Attribute
    {
        public " + CloneAttributeConstants.CloneAttributeString + @"()
        {
        }

        public bool " + CloneAttributeConstants.PreventDeepCopyKeyString + @" { get; set; }
    }
}
";
    
    public AttributeInfo AttributeInfo { get; } = new()
    {
        Name = CloneAttributeConstants.CloneAttributeString,
        SourceText = SourceText.From(Text, Encoding.UTF8)
    };
}