using System.Text;
using Cloneable.Generators.Attributes.Abstraction;
using Microsoft.CodeAnalysis.Text;

namespace Cloneable.Generators.Attributes.IgnoreClone;

internal class IgnoreCloneAttributeProvider : IAttributeProvider
{
    private const string Text = @"using System;

namespace " + CloneableConstants.RootNamespace + @"
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class " + IgnoreCloneAttributeConstants.IgnoreCloneAttributeString + @" : Attribute
    {
        public " + IgnoreCloneAttributeConstants.IgnoreCloneAttributeString + @"()
        {
        }
    }
}
";
    
    public AttributeInfo AttributeInfo { get; } = new()
    {
        Name = IgnoreCloneAttributeConstants.IgnoreCloneAttributeString,
        SourceText = SourceText.From(Text, Encoding.UTF8)
    };
}