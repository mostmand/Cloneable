using System.Collections.Generic;

namespace Cloneable.Generators.Attributes.Abstraction;

internal interface IAttributeFinder
{
    IReadOnlyCollection<AttributeInfo> FindAttributes();
}