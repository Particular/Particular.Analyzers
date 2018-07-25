namespace Particular.CodeRules.ConfigureAwait
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    /// <summary>
    ///     The analyzer to identify any missing ConfigureAwaits
    /// </summary>
    /// <seealso cref="Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer" />
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConfigureAwaitAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        ///     Gets the list of supported diagnostics for the analyzer.
        /// </summary>
        /// <value>
        ///     The supported diagnostics.
        /// </value>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.UseConfigureAwait);

        /// <summary>
        ///     Initializes the specified analyzer on the <paramref name="context" />.
        /// </summary>
        /// <param name="context">The context.</param>
        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeForConfigureFalse, SyntaxKind.AwaitExpression);
        }

        private void AnalyzeForConfigureFalse(SyntaxNodeAnalysisContext context)
        {
            if (context.IsFromGeneratedCode())
            {
                return;
            }

            var node = (AwaitExpressionSyntax)context.Node;
            if (node.Expression != null)
            {
                var type = ModelExtensions.GetTypeInfo(context.SemanticModel, node.Expression).Type;
                if (type.ContainingNamespace?.ToString() == "System.Threading.Tasks" && type.Name == "Task")
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.UseConfigureAwait, node.GetLocation()));
                }
            }
        }
    }
}