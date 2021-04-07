namespace Particular.CodeRules.Cancellation
{
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Particular.CodeRules.Extensions;
    using System;

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
            if (!(context.Node is MemberDeclarationSyntax member))
            {
                return;
            }

            if (!(context.SemanticModel.GetMethod(member, context.CancellationToken, out _) is IMethodSymbol method))
            {
                return;
            }

            Analyze(context, method.Parameters);
        }

        static void Analyze(SyntaxNodeAnalysisContext context, ImmutableArray<IParameterSymbol> parameters)
        {
            // cheapest checks first
            if (!parameters.Any())
            {
                return;
            }

            foreach (var token in parameters
                .Where(param => param.Type.IsCancellationToken()))
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
