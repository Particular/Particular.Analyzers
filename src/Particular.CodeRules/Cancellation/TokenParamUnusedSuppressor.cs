namespace Particular.CodeRules.Cancellation
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Particular.CodeRules.Extensions;

    // see https://github.com/dotnet/roslyn/pull/36067
    // TODO: add tests
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TokenParamUnusedSuppressor : DiagnosticSuppressor
    {
        public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(
            SuppressionDescriptors.CancellationTokenParameterUnused);

        public override void ReportSuppressions(SuppressionAnalysisContext context)
        {
            foreach (var diagnostic in context.ReportedDiagnostics)
            {
                var node = diagnostic.Location.SourceTree.GetRoot(context.CancellationToken).FindNode(diagnostic.Location.SourceSpan);

                if (node == null)
                {
                    continue;
                }

                if (!(node is ParameterSyntax parameter))
                {
                    continue;
                }

                // cheapest checks first
                if (!(parameter.Type is NameSyntax))
                {
                    return;
                }

                if (context.GetSemanticModel(node.SyntaxTree).GetTypeInfo(parameter.Type).Type.IsCancellationToken())
                {
                    context.ReportSuppression(SuppressionDescriptors.CancellationTokenParameterUnused, diagnostic);
                }
            }
        }
    }
}
