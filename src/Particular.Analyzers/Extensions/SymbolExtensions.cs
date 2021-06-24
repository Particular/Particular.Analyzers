namespace Particular.Analyzers.Extensions
{
    using Microsoft.CodeAnalysis;

    public static class SymbolExtensions
    {
        public static IMethodSymbol GetMethodFromSymbol(this ISymbol expression)
        {
            switch (expression)
            {
                case IFieldSymbol symbol when symbol.Type is INamedTypeSymbol type:
                    return type.DelegateInvokeMethod;
                case ILocalSymbol symbol when symbol.Type is INamedTypeSymbol type:
                    return type.DelegateInvokeMethod;
                case IMethodSymbol symbol:
                    return symbol;
                case IParameterSymbol symbol when symbol.Type is INamedTypeSymbol type:
                    return type.DelegateInvokeMethod;
                case IPropertySymbol symbol when symbol.Type is INamedTypeSymbol type:
                    return type.DelegateInvokeMethod;
                default:
                    return null;
            }
        }
    }
}
