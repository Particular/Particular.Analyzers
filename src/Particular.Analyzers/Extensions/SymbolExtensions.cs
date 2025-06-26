namespace Particular.Analyzers.Extensions
{
    using Microsoft.CodeAnalysis;

    public static class SymbolExtensions
    {
        public static IMethodSymbol? GetMethodOrDefault(this ISymbol symbol) => symbol switch
        {
            IFieldSymbol field when field.Type is INamedTypeSymbol type => type.DelegateInvokeMethod,
            ILocalSymbol local when local.Type is INamedTypeSymbol type => type.DelegateInvokeMethod,
            IMethodSymbol method => method,
            IParameterSymbol param when param.Type is INamedTypeSymbol type => type.DelegateInvokeMethod,
            IPropertySymbol property when property.Type is INamedTypeSymbol type => type.DelegateInvokeMethod,
            _ => null,
        };
    }
}
