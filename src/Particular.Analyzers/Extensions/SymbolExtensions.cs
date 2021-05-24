namespace Particular.Analyzers.Extensions
{
    using Microsoft.CodeAnalysis;

    public static class SymbolExtensions
    {
        public static ITypeSymbol GetTypeSymbolOrDefault(this ISymbol symbol)
        {
            switch (symbol)
            {
                case IDiscardSymbol symbolWithType:
                    return symbolWithType.Type;
                case IEventSymbol symbolWithType:
                    return symbolWithType.Type;
                case IFieldSymbol symbolWithType:
                    return symbolWithType.Type;
                case ILocalSymbol symbolWithType:
                    return symbolWithType.Type;
                case IMethodSymbol symbolWithType:
                    return symbolWithType.ReturnType;
                case INamedTypeSymbol symbolWithType:
                    return symbolWithType;
                case IParameterSymbol symbolWithType:
                    return symbolWithType.Type;
                case IPointerTypeSymbol symbolWithType:
                    return symbolWithType.PointedAtType;
                case IPropertySymbol symbolWithType:
                    return symbolWithType.Type;
                case ITypeSymbol symbolWithType:
                    return symbolWithType;
                default:
                    return null;
            }
        }

    }
}
