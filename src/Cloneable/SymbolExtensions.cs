using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace Cloneable
{
    internal static class SymbolExtensions
    {
        public static bool TryGetAttribute(this ISymbol symbol, INamedTypeSymbol attributeType, out IEnumerable<AttributeData> attributes)
        {
            attributes = symbol.GetAttributes()
                .Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeType));
            return attributes.Any();
        }

        public static bool HasAttribute(this ISymbol symbol, INamedTypeSymbol attributeType)
        {
            return symbol.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeType));
        }
    }
}
