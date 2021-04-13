namespace Particular.Analyzers.AwaitOrCaptureTasks
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AwaitOrCaptureTasksAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        ///     Gets the list of supported diagnostics for the analyzer.
        /// </summary>
        /// <value>
        ///     The supported diagnostics.
        /// </value>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.AwaitOrCaptureTasks);

        /// <summary>
        ///     Initializes the specified analyzer on the <paramref name="context" />.
        /// </summary>
        /// <param name="context">The context.</param>
        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node as InvocationExpressionSyntax;
            var parentNode = node?.Parent as ExpressionStatementSyntax;

            if (parentNode != null)
            {
                var symbol = context.SemanticModel.GetSymbolInfo(node.Expression).Symbol;

                if (IsDroppedTask(context, symbol))
                {
                    var location = node.GetLocation();
                    var diagnostic = Diagnostic.Create(DiagnosticDescriptors.AwaitOrCaptureTasks, location);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static bool IsDroppedTask(SyntaxNodeAnalysisContext context, ISymbol symbol)
        {
            if (symbol is IMethodSymbol)
            {
                return DerivesFromTask(context, ((IMethodSymbol)symbol).ReturnType);
            }

            if (symbol is ILocalSymbol)
            {
                // Possibly a Func or delegate that returns a Task
                var namedType = ((ILocalSymbol)symbol).Type as INamedTypeSymbol;
                if (namedType?.TypeKind == TypeKind.Delegate)
                {
                    var delegateInvoke = namedType.DelegateInvokeMethod;
                    var returnType = delegateInvoke.ReturnType;
                    return DerivesFromTask(context, returnType);
                }
            }

            return false;
        }

        private static bool DerivesFromTask(SyntaxNodeAnalysisContext context, ITypeSymbol symbol)
        {
            while (symbol != null)
            {
                if (symbol.ToString() == "System.Threading.Tasks.Task")
                {
                    return true;
                }

                symbol = symbol.BaseType;
            }

            return false;
        }
    }
}
