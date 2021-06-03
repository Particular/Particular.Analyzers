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
    public class NonPrivateMethodTokenNameAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.NonPrivateMethodSingleCancellationTokenMisnamed);

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
            if (declaredSymbol.DeclaredAccessibility == Accessibility.Private && method.MethodKind != MethodKind.ExplicitInterfaceImplementation)
            {
                // covered by MethodTokenNamesAnalyzer
                return;
            }

            if (method.Parameters.IsDefaultOrEmpty)
            {
                return;
            }

            var tokens = method.Parameters.Where(param => param.Type.IsCancellationToken()).Take(2).ToList();

            if (tokens.Count != 1)
            {
                // covered by MethodTokenNamesAnalyzer
                return;
            }

            var token = tokens.Single();

            if (token.Name == "cancellationToken")
            {
                return;
            }

            context.ReportDiagnostic(DiagnosticDescriptors.NonPrivateMethodSingleCancellationTokenMisnamed, token);
        }
    }
}
