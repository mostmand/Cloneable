using System;
using System.Collections.Generic;
using System.Linq;
using Cloneable.Generators.Attributes.Abstraction;

namespace Cloneable.Generators.Attributes;

internal class AttributeFinder : IAttributeFinder
{
    private static readonly List<IAttributeProvider> AttributeProviders = FindAttributeProviders();

    public IReadOnlyCollection<AttributeInfo> FindAttributes()
    {
        return AttributeProviders.Select(x => x.AttributeInfo)
            .ToArray();
    }

    private static List<IAttributeProvider> FindAttributeProviders()
    {
        return typeof(IAssemblyMarker).Assembly
            .DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false } &&
                           typeof(IAttributeProvider).IsAssignableFrom(type))
            .Select(Activator.CreateInstance)
            .Cast<IAttributeProvider>()
            .ToList();
    }
}