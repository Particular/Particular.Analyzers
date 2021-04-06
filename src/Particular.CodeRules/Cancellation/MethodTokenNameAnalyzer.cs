namespace Particular.CodeRules.Cancellation
{
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Particular.CodeRules.Extensions;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MethodTokenNameAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.MethodCancellationTokenMisnamed);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
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

            Analyze(context, method);
        }

        static void Analyze(SyntaxNodeAnalysisContext context, IMethodSymbol method)
        {
            // cheapest checks first
            if (!method.Parameters.Any())
            {
                return;
            }

            var tokens = method.Parameters.Where(param => param.Type.IsCancellationToken()).Take(2).ToList();

            if (tokens.Count != 1)
            {
                return;
            }

            var token = tokens.Single();

            if (token.Name == "cancellationToken")
            {
                return;
            }

            // TODO: split into two, one for private, one for non-private
            // if (declaredSymbol.DeclaredAccessibility == Accessibility.Private && token.Name.EndsWith("CancellationToken", StringComparison.Ordinal))
            context.ReportDiagnostic(DiagnosticDescriptors.MethodCancellationTokenMisnamed, token);
        }
    }
}
