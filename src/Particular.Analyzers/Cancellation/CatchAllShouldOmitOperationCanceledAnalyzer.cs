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
                    if (CatchFiltersOutOperationCanceled(catchClause))
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

        static bool CatchFiltersOutOperationCanceled(CatchClauseSyntax catchClause)
        {
            var filterClause = catchClause.ChildNodes().OfType<CatchFilterClauseSyntax>().FirstOrDefault();
            if (filterClause == null)
            {
                return false;
            }

            if (filterClause.DescendantNodes().OfType<IdentifierNameSyntax>().Any(name => name.Identifier.ValueText == "OperationCanceledException"))
            {
                return true;
            }

            return false;
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
