namespace Particular.Analyzers.Cancellation
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Particular.Analyzers.Extensions;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TokenAccessibilityAnalyzer : DiagnosticAnalyzer
    {
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(
                Analyze,
                SyntaxKind.DelegateDeclaration,
                SyntaxKind.MethodDeclaration);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.CancellationTokenNonPrivateRequired,
            DiagnosticDescriptors.CancellationTokenPrivateOptional);

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
            if (method.MethodKind == MethodKind.ExplicitInterfaceImplementation)
            {
                return;
            }

            foreach (var param in method.Parameters)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                Analyze(context, param, declaredSymbol.DeclaredAccessibility);
            }
        }

        static void Analyze(SyntaxNodeAnalysisContext context, IParameterSymbol param, Accessibility accessibility)
        {
            if (!param.Type.IsCancellationToken())
            {
                return;
            }

            if (accessibility == Accessibility.Private && param.IsOptional)
            {
                context.ReportDiagnostic(DiagnosticDescriptors.CancellationTokenPrivateOptional, param);
            }
            else if (accessibility != Accessibility.Private && !param.IsOptional)
            {
                context.ReportDiagnostic(DiagnosticDescriptors.CancellationTokenNonPrivateRequired, param);
            }
        }
    }
}
