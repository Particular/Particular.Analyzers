namespace Particular.Analyzers.Extensions
{
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    static class SemanticModelExtensions
    {
        public static IMethodSymbol? GetMethod(this SemanticModel semanticModel, MemberDeclarationSyntax declarationSyntax, CancellationToken cancellationToken, out ISymbol? declaredSymbol)
        {
            switch (declarationSyntax)
            {
                case BaseMethodDeclarationSyntax methodSyntax:
                    var method = semanticModel.GetDeclaredSymbol(methodSyntax, cancellationToken);
                    declaredSymbol = method;
                    return method;
                case DelegateDeclarationSyntax delegateSyntax:
                    return semanticModel.GetInvokeMethod(delegateSyntax, cancellationToken, out declaredSymbol);
                default:
                    declaredSymbol = null;
                    return null;
            }
        }

        public static IMethodSymbol? GetInvokeMethod(this SemanticModel semanticModel, DelegateDeclarationSyntax declarationSyntax, CancellationToken cancellationToken, out ISymbol? declaredSymbol)
        {
            var @delegate = semanticModel.GetDeclaredSymbol(declarationSyntax, cancellationToken);
            declaredSymbol = @delegate;
            return @delegate?.DelegateInvokeMethod;
        }
    }
}
