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
            context.RegisterCompilationStartAction(startContext =>
            {
                if (!(startContext.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken") is INamedTypeSymbol cancellationTokenType))
                {
                    return;
                }

                startContext.RegisterSyntaxNodeAction(
                    analyzeContext => Analyze(analyzeContext, cancellationTokenType),
                    SyntaxKind.TryStatement);
            });
        }

        static void Analyze(SyntaxNodeAnalysisContext context, INamedTypeSymbol cancellationTokenType)
        {
            if (!(context.Node is TryStatementSyntax tryStatement))
            {
                return;
            }

            foreach (var catchClause in tryStatement.ChildNodes().OfType<CatchClauseSyntax>())
            {
                var catchType = GetCatchType(catchClause);

                if (catchType == "OperationCanceledException")
                {
                    return;
                }

                if (catchType == "Exception" || catchType == "System.Exception")
                {
                    if (CatchFiltersOutOperationCanceled(catchClause, context))
                    {
                        return;
                    }

                    var tryBlockCalls = tryStatement.ChildNodes().OfType<BlockSyntax>()
                        .SelectMany(block => block.DescendantNodes().OfType<InvocationExpressionSyntax>());

                    foreach (var call in tryBlockCalls)
                    {
                        if (call.ArgumentList.Arguments.Any(arg => IsCancellationToken(arg.Expression, context, cancellationTokenType)))
                        {
                            context.ReportDiagnostic(DiagnosticDescriptors.CatchAllShouldOmitOperationCanceled, catchClause.CatchKeyword);
                            return;
                        }
                    }

                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "<Pending>")]
        static bool CatchFiltersOutOperationCanceled(CatchClauseSyntax catchClause, SyntaxNodeAnalysisContext context)
        {
            var filterClause = catchClause.ChildNodes().OfType<CatchFilterClauseSyntax>().FirstOrDefault();
            if (filterClause == null)
            {
                return false;
            }

            var logicalNotExpression = filterClause.ChildNodes().OfType<PrefixUnaryExpressionSyntax>().FirstOrDefault();
            if (logicalNotExpression == null || !logicalNotExpression.IsKind(SyntaxKind.LogicalNotExpression))
            {
                return false;
            }

            var parenthesizedExpression = logicalNotExpression.ChildNodes().OfType<ParenthesizedExpressionSyntax>().FirstOrDefault();
            if (parenthesizedExpression == null)
            {
                return false;
            }

            var binaryExpression = parenthesizedExpression.ChildNodes().OfType<BinaryExpressionSyntax>().FirstOrDefault();
            if (binaryExpression == null)
            {
                return false;
            }

            var isKeyword = binaryExpression.OperatorToken.IsKind(SyntaxKind.IsKeyword);
            var leftSymbol = context.SemanticModel.GetSymbolInfo(binaryExpression.Left, context.CancellationToken).Symbol as ILocalSymbol;
            var rightSymbol = context.SemanticModel.GetSymbolInfo(binaryExpression.Right, context.CancellationToken).Symbol as INamedTypeSymbol;

            var leftIsException = leftSymbol?.Type.ToString() == "System.Exception";
            var rightIsOpCanceled = rightSymbol?.ToString() == "System.OperationCanceledException";

            return isKeyword && leftIsException && rightIsOpCanceled;
        }

        static bool IsCancellationToken(ExpressionSyntax expressionSyntax, SyntaxNodeAnalysisContext context, INamedTypeSymbol cancellationTokenType)
        {
            var expressionSymbol = context.SemanticModel.GetSymbolInfo(expressionSyntax, context.CancellationToken).Symbol;
            return SymbolEqualityComparer.Default.Equals(expressionSymbol.GetTypeSymbolOrDefault(), cancellationTokenType);
        }

        static string GetCatchType(CatchClauseSyntax catchClause)
        {
            var catchDeclaration = catchClause.ChildNodes().OfType<CatchDeclarationSyntax>().FirstOrDefault();

            // This means:
            //   catch
            //   {
            //   }
            if (catchDeclaration == null)
            {
                return "Exception";
            }

            var identifier = catchDeclaration.ChildNodes().OfType<NameSyntax>().FirstOrDefault();

            return identifier?.ToString();
        }
    }
}
