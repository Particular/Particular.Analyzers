namespace Particular.Analyzers.Extensions
{
    using Microsoft.CodeAnalysis;

    public static class SymbolExtensions
    {
        public static IMethodSymbol GetMethodOrDefault(this ISymbol symbol)
        {
            switch (symbol)
            {
                case IFieldSymbol field when field.Type is INamedTypeSymbol type:
                    return type.DelegateInvokeMethod;
                case ILocalSymbol local when local.Type is INamedTypeSymbol type:
                    return type.DelegateInvokeMethod;
                case IMethodSymbol method:
                    return method;
                case IParameterSymbol param when param.Type is INamedTypeSymbol type:
                    return type.DelegateInvokeMethod;
                case IPropertySymbol property when property.Type is INamedTypeSymbol type:
                    return type.DelegateInvokeMethod;
                default:
                    return null;
            }
        }
    }
}
