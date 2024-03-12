namespace Particular.Analyzers.Cancellation
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Particular.Analyzers.Extensions;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MethodTokenNamesAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.MethodCancellationTokenMisnamed);

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
            if (context.Node is not MemberDeclarationSyntax member)
            {
                return;
            }

            if (context.SemanticModel.GetMethod(member, context.CancellationToken, out var declaredSymbol) is not IMethodSymbol method || declaredSymbol is null)
            {
                return;
            }

            Analyze(context, method, declaredSymbol);
        }

        static void Analyze(SyntaxNodeAnalysisContext context, IMethodSymbol method, ISymbol declaredSymbol)
        {
            // cheapest checks first
            if (method.Parameters.IsDefaultOrEmpty)
            {
                return;
            }

            var tokens = method.Parameters.Where(param => param.Type.IsCancellationToken()).ToList();

            if (tokens.Count == 1 && (declaredSymbol.DeclaredAccessibility != Accessibility.Private || method.MethodKind == MethodKind.ExplicitInterfaceImplementation))
            {
                // covered by NonPrivateMethodTokenNameAnalyzer
                return;
            }

            foreach (var token in tokens)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                Analyze(context, token);
            }
        }

        static void Analyze(SyntaxNodeAnalysisContext context, IParameterSymbol token)
        {
            if (token.Name == "cancellationToken")
            {
                return;
            }

            if (token.Name.EndsWith("CancellationToken", StringComparison.Ordinal))
            {
                return;
            }

            context.ReportDiagnostic(DiagnosticDescriptors.MethodCancellationTokenMisnamed, token);
        }
    }
}
