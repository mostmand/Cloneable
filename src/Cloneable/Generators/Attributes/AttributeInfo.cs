using Microsoft.CodeAnalysis.Text;

namespace Cloneable.Generators.Attributes;

internal class AttributeInfo
{
    public string Name { get; init; }
    public SourceText SourceText { get; init; }
}