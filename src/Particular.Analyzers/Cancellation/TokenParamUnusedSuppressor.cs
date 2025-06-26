namespace Particular.Analyzers.Cancellation
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Particular.Analyzers.Extensions;

    // see https://github.com/dotnet/roslyn/pull/36067
    // there are no tests for this class because
    // it's not clear how to get a instance of
    // the analyzer that reports IDE0060
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TokenParamUnusedSuppressor : DiagnosticSuppressor
    {
        public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => [SuppressionDescriptors.CancellationTokenParameterUnused];

        public override void ReportSuppressions(SuppressionAnalysisContext context)
        {
            foreach (var diagnostic in context.ReportedDiagnostics)
            {
                var sourceTree = diagnostic.Location.SourceTree;
                if (sourceTree is null)
                {
                    continue;
                }

                var node = sourceTree.GetRoot(context.CancellationToken).FindNode(diagnostic.Location.SourceSpan);
                if (node == null)
                {
                    continue;
                }

                if (node is not ParameterSyntax parameter)
                {
                    continue;
                }

                // cheapest checks first
                if (parameter.Type is not NameSyntax)
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
