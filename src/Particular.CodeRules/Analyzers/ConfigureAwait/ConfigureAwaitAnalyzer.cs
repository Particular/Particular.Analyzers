using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Particular.CodeRules.Analyzers.ConfigureAwait
{
    /// <summary>
    ///     The analyzer to identify any missing ConfigureAwaits
    /// </summary>
    /// <seealso cref="Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer" />
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConfigureAwaitAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        ///     The rule identifier for the configure await analyzer. Particular Code Rules 0001
        /// </summary>
        public const string RuleIdentifier = "PCR0001";

        internal static DiagnosticDescriptor RuleDescriptor = new DiagnosticDescriptor(RuleIdentifier,
            "Await used without specifying ConfigureAwait", "Await used without specifying ConfigureAwait", "Usage",
            DiagnosticSeverity.Warning, true);

        /// <summary>
        ///     Gets the list of supported diagnostics for the analyzer.
        /// </summary>
        /// <value>
        ///     The supported diagnostics.
        /// </value>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RuleDescriptor);

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
            var node = (AwaitExpressionSyntax) context.Node;
            if (node.Expression != null)
            {
                var type = ModelExtensions.GetTypeInfo(context.SemanticModel, node.Expression).Type;
                if (type.ContainingNamespace.ToString() == "System.Threading.Tasks" && type.Name == "Task")
                {
                    context.ReportDiagnostic(Diagnostic.Create(RuleDescriptor, node.GetLocation()));
                }
            }
        }
    }
}