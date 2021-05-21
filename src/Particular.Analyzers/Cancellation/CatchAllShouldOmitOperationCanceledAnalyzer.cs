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
    public class CatchAllShouldOmitOperationCanceledAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.CatchAllShouldOmitOperationCanceled);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.TryStatement);
        }

        static void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is TryStatementSyntax tryStatement))
            {
                return;
            }

            foreach (var catchClause in tryStatement.Catches)
            {
                var catchType = GetCatchType(catchClause);

                if (catchType == "OperationCanceledException" || catchType == "System.OperationCanceledException")
                {
                    return;
                }

                if (catchType != "Exception" && catchType != "System.Exception")
                {
                    continue;
                }

                if (CatchFiltersOutOperationCanceled(catchClause, context))
                {
                    return;
                }

                // Because we are examining all descendants, this may result in false positives.
                // For example, a nested try block may contain cancellable invocations and
                // a related catch block may swallow OperationCanceledException.
                // Or, an anonymous delegate may contain cancellable invocations but
                // may not actually be executed in the try block.
                // However, these are edge cases and would be complicated to analyze.
                // In these cases, either the fix can be redundantly applied, or the analyzer can be suppressed.
                var tryBlockCalls = tryStatement.Block.DescendantNodes().OfType<InvocationExpressionSyntax>();

                foreach (var call in tryBlockCalls)
                {
                    if (call.ArgumentList.Arguments
                        .Select(arg => context.SemanticModel.GetTypeInfo(arg.Expression, context.CancellationToken).Type)
                        .Any(arg => arg.IsCancellationToken() || arg.IsCancellableContext()))
                    {
                        context.ReportDiagnostic(DiagnosticDescriptors.CatchAllShouldOmitOperationCanceled, catchClause.CatchKeyword);
                        return;
                    }
                }
            }
        }

        static string GetCatchType(CatchClauseSyntax catchClause)
        {
            // This means:
            //   catch
            //   {
            //   }
            if (catchClause.Declaration == null)
            {
                return "Exception";
            }

            return catchClause.Declaration.Type.ToString();
        }

        static bool CatchFiltersOutOperationCanceled(CatchClauseSyntax catchClause, SyntaxNodeAnalysisContext context)
        {
            if (catchClause.Filter == null)
            {
                return false;
            }

            if (catchClause.Filter.FilterExpression is PrefixUnaryExpressionSyntax prefixUnaryExpression)
            {
                // C# < 9 pattern: when (!(ex is OperationCanceledException))
                return Verify(prefixUnaryExpression, catchClause, context);
            }

            if (catchClause.Filter.FilterExpression is IsPatternExpressionSyntax isPatternExpression)
            {
                // C# 9 pattern: when (ex is not OperationCanceledException)
                return Verify(isPatternExpression, catchClause, context);
            }

            return false;
        }

        /// <summary>
        /// Ensures `{prefix}({expression})` is `!(ex is OperationCanceledException)`
        /// </summary>
        static bool Verify(PrefixUnaryExpressionSyntax logicalNotExpression, CatchClauseSyntax catchClause, SyntaxNodeAnalysisContext context)
        {
            // cheapest checks first
            if (!logicalNotExpression.IsKind(SyntaxKind.LogicalNotExpression))
            {
                return false;
            }

            if (!(logicalNotExpression.ChildNodes().FirstOrDefault() is ParenthesizedExpressionSyntax parenthesizedExpression))
            {
                return false;
            }

            if (!(parenthesizedExpression.ChildNodes().FirstOrDefault() is BinaryExpressionSyntax binaryExpression))
            {
                return false;
            }

            if (!binaryExpression.OperatorToken.IsKind(SyntaxKind.IsKeyword))
            {
                return false;
            }

            // Now evaluate symbols
            var leftSymbol = context.SemanticModel.GetSymbolInfo(binaryExpression.Left, context.CancellationToken).Symbol as ILocalSymbol;

            if (leftSymbol?.Name != catchClause.Declaration.Identifier.Text)
            {
                return false;
            }

            var rightSymbol = context.SemanticModel.GetSymbolInfo(binaryExpression.Right, context.CancellationToken).Symbol as INamedTypeSymbol;

            return rightSymbol?.ToString() == "System.OperationCanceledException";
        }

        /// <summary>
        /// Ensures `{symbol} is {pattern}` is `ex is not OperationCanceledException`
        /// </summary>
        static bool Verify(IsPatternExpressionSyntax isPatternExpression, CatchClauseSyntax catchClause, SyntaxNodeAnalysisContext context)
        {
            // Cheaper to evaluate this before going left->right and getting symbol info
            if (!(isPatternExpression.Pattern is UnaryPatternSyntax notPattern && notPattern.IsKind(SyntaxKind.NotPattern)))
            {
                return false;
            }

            if (!(notPattern.Pattern is ConstantPatternSyntax constantPattern))
            {
                return false;
            }

            // Now evaluate symbols
            var leftSymbol = context.SemanticModel.GetSymbolInfo(isPatternExpression.Expression, context.CancellationToken).Symbol as ILocalSymbol;

            if (leftSymbol?.Name != catchClause.Declaration.Identifier.Text)
            {
                return false;
            }

            var rightSymbol = context.SemanticModel.GetSymbolInfo(constantPattern.Expression, context.CancellationToken).Symbol as INamedTypeSymbol;

            return rightSymbol?.ToString() == "System.OperationCanceledException";
        }
    }
}
