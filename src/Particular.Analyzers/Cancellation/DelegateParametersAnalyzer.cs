namespace Particular.Analyzers.Cancellation
{
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Particular.Analyzers.Extensions;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DelegateParametersAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.DelegateCancellationTokenMisplaced);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.DelegateDeclaration);
        }

        static void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not DelegateDeclarationSyntax delegateSyntax)
            {
                return;
            }

            // cheapest checks first
            if (!delegateSyntax.ParameterList.Parameters.Any())
            {
                return;
            }

            if (!delegateSyntax.ParameterList.Parameters.Any(param => param.Type is NameSyntax))
            {
                return;
            }

            Analyze(context, context.SemanticModel.GetInvokeMethod(delegateSyntax, context.CancellationToken, out _).Parameters);
        }

        static void Analyze(SyntaxNodeAnalysisContext context, ImmutableArray<IParameterSymbol> @params)
        {
            var tokenAllowed = true;

            for (var index = @params.Length - 1; index >= 0; --index)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var param = @params[index];

                if (param.Type.IsCancellationToken())
                {
                    if (tokenAllowed)
                    {
                        continue;
                    }

                    context.ReportDiagnostic(DiagnosticDescriptors.DelegateCancellationTokenMisplaced, param);
                }

                if (AllowedAfterToken(param))
                {
                    continue;
                }

                tokenAllowed = false;
            }
        }

        static bool AllowedAfterToken(IParameterSymbol param) =>
            param.IsOptional ||
            param.IsParams ||
            param.RefKind == RefKind.Ref ||
            param.RefKind == RefKind.RefReadOnly ||
            param.RefKind == RefKind.Out;
    }
}
