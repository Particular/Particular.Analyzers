namespace Particular.Analyzers.Cancellation
{
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Particular.Analyzers.Extensions;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MethodParametersAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.MethodCancellationTokenMisnamed,
            DiagnosticDescriptors.MethodMixedCancellation,
            DiagnosticDescriptors.MethodMultipleCancellableContexts,
            DiagnosticDescriptors.MethodMultipleCancellationTokens);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(
                Analyze,
                SyntaxKind.ConstructorDeclaration,
                SyntaxKind.DelegateDeclaration,
                SyntaxKind.MethodDeclaration);
        }

        static void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is MemberDeclarationSyntax member))
            {
                return;
            }

            if (!(context.SemanticModel.GetMethod(member, context.CancellationToken, out var declaredSymbol) is IMethodSymbol method))
            {
                return;
            }

            Analyze(context, method, declaredSymbol);
        }

        static void Analyze(SyntaxNodeAnalysisContext context, IMethodSymbol method, ISymbol declaredSymbol)
        {
            // cheapest checks first
            if (method.IsOverride)
            {
                return;
            }

            if (method.MethodKind == MethodKind.ExplicitInterfaceImplementation)
            {
                return;
            }

            if (method.Parameters.IsDefaultOrEmpty)
            {
                return;
            }

            var tokens = method.Parameters.Where(param => param.Type.IsCancellationToken()).Take(2).Count();
            var contexts = method.Parameters.Where(param => param.Type.IsCancellableContext()).Take(2).Count();

            if (tokens > 1)
            {
                context.ReportDiagnostic(DiagnosticDescriptors.MethodMultipleCancellationTokens, declaredSymbol);
            }

            if (contexts > 1)
            {
                context.ReportDiagnostic(DiagnosticDescriptors.MethodMultipleCancellableContexts, declaredSymbol);
            }

            if (tokens > 0 && contexts > 0)
            {
                context.ReportDiagnostic(DiagnosticDescriptors.MethodMixedCancellation, declaredSymbol);
            }
        }
    }
}
