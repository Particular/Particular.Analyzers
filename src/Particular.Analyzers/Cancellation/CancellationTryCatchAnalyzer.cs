namespace Particular.Analyzers.Cancellation
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Particular.Analyzers.Extensions;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CancellationTryCatchAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.ImproperTryCatchSystemException,
            DiagnosticDescriptors.ImproperTryCatchOperationCanceled,
            DiagnosticDescriptors.MultipleCancellationTokensInATry);

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

            var catches = tryStatement.Catches
                .Select(catchClause => (catchClause, catchType: GetCatchType(catchClause)))
                .Where(tuple => tuple.catchType == "Exception" || tuple.catchType == "OperationCanceledException")
                .ToImmutableArray();

            if (!catches.Any())
            {
                // No catch blocks of interest
                return;
            }

            var tokenExpressionString = GetActiveCancellationTokenExpressionString(context, tryStatement);
            if (tokenExpressionString == null)
            {
                return;
            }

            foreach (var (catchClause, catchType) in catches)
            {
                if (catchType == "OperationCanceledException")
                {
                    if (!CatchFiltersByIsCancellationRequested(catchClause, tokenExpressionString))
                    {
                        context.ReportDiagnostic(DiagnosticDescriptors.ImproperTryCatchOperationCanceled, catchClause.CatchKeyword);
                    }
                    return;
                }

                if (catchType == "Exception")
                {
                    if (CatchIncludesCancellationTokenExpression(catchClause, tokenExpressionString))
                    {
                        if (CatchIncludesSameException(catchClause))
                        {
                            return;
                        }
                    }

                    context.ReportDiagnostic(DiagnosticDescriptors.ImproperTryCatchSystemException, catchClause.CatchKeyword);
                    return;
                }
            }
        }



        static bool CatchFiltersByIsCancellationRequested(CatchClauseSyntax catchClause, string tokenExpressionString)
        {
            if (catchClause.Filter == null)
            {
                return false;
            }

            if (!(catchClause.Filter.FilterExpression is MemberAccessExpressionSyntax memberAccess) || !memberAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                return false;
            }

            if (memberAccess.Name.Identifier.ValueText != "IsCancellationRequested")
            {
                return false;
            }

            return memberAccess.Expression.ToString() == tokenExpressionString;
        }

        static bool CatchIncludesCancellationTokenExpression(CatchClauseSyntax catchClause, string tokenExpressionString)
        {
            if (catchClause.Filter == null)
            {
                return false;
            }

            var tokens = catchClause.Filter.DescendantNodes()
                .Where(node => node.ToString() == tokenExpressionString);

            return tokens.Any();
        }

        static bool CatchIncludesSameException(CatchClauseSyntax catchClause)
        {
            var identifier = catchClause.Declaration?.Identifier.Text;
            if (identifier == null)
            {
                // just `catch (Exception)` or even `catch { }` - no variable name
                return false;
            }

            if (catchClause.Filter == null)
            {
                return false;
            }

            var filterIdentifiers = catchClause.Filter.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Where(nameSyntax => nameSyntax.Identifier.ValueText == identifier);

            return filterIdentifiers.Any();
        }

        static string GetActiveCancellationTokenExpressionString(SyntaxNodeAnalysisContext context, TryStatementSyntax tryStatement)
        {
            // Because we are examining all descendants, this may result in false positives.
            // For example, a nested try block may contain cancellable invocations and
            // a related catch block may swallow OperationCanceledException.
            // Or, an anonymous delegate may contain cancellable invocations but
            // may not actually be executed in the try block.
            // However, these are edge cases and would be complicated to analyze.
            // In these cases, either the fix can be redundantly applied, or the analyzer can be suppressed.
            var tryBlockCalls = tryStatement.Block.DescendantNodes().OfType<InvocationExpressionSyntax>();

            var distinctTokenArguments = GetTokenArguments(context, tryBlockCalls)
                .Distinct()
                .ToImmutableArray();

            if (distinctTokenArguments.Length > 1)
            {
                context.ReportDiagnostic(DiagnosticDescriptors.MultipleCancellationTokensInATry, tryStatement.TryKeyword);
            }

            return distinctTokenArguments.FirstOrDefault();
        }

        static IEnumerable<string> GetTokenArguments(SyntaxNodeAnalysisContext context, IEnumerable<InvocationExpressionSyntax> calls)
        {
            foreach (var call in calls)
            {
                if (call.Expression is MemberAccessExpressionSyntax memberAccess && memberAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                {
                    if (memberAccess.Name.Identifier.ValueText == "ThrowIfCancellationRequested")
                    {
                        if (context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken).Type.IsCancellationToken())
                        {
                            yield return memberAccess.Expression.ToString();
                        }
                    }
                }

                var tokenArguments = call.ArgumentList.Arguments
                    .Where(arg => !(arg.Expression is LiteralExpressionSyntax))
                    .Where(arg => !(arg.Expression is DefaultExpressionSyntax))
                    .Where(arg => !IsCancellationTokenNone(arg))
                    .Select(arg =>
                    {
                        var type = context.SemanticModel.GetTypeInfo(arg.Expression, context.CancellationToken).Type;
                        if (type.IsCancellationToken())
                        {
                            return arg.Expression.ToString();
                        }

                        if (type.IsCancellableContext())
                        {
                            return arg.Expression.ToString() + ".CancellationToken";
                        }

                        return null;
                    })
                    .Where(expr => expr != null);

                foreach (var expressionString in tokenArguments)
                {
                    yield return expressionString;
                }
            }
        }

        static string GetCatchType(CatchClauseSyntax catchClause)
        {
            // if catchClause.Declaration is null, that means:
            //   catch
            //   {
            //   }

            switch (catchClause.Declaration?.Type.ToString())
            {
                case null:
                case "Exception":
                case "System.Exception":
                    return "Exception";
                case "OperationCanceledException":
                case "System.OperationCanceledException":
                    return "OperationCanceledException";
                default:
                    return null;
            }
        }

        static bool IsCancellationTokenNone(ArgumentSyntax arg)
        {
            if (!(arg.Expression is MemberAccessExpressionSyntax memberAccess))
            {
                return false;
            }

            if (!memberAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                return false;
            }

            if (!(memberAccess.Expression is SimpleNameSyntax @ref))
            {
                return false;
            }

            return @ref.Identifier.ValueText == "CancellationToken" && memberAccess.Name.Identifier.ValueText == "None";
        }
    }
}
