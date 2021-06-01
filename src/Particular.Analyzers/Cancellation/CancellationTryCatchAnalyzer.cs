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
        static readonly string ExceptionType = "System.Exception";
        static readonly string OperationCanceledExceptionType = "System.OperationCanceledException";

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
                .Where(tuple => tuple.catchType == ExceptionType || tuple.catchType == OperationCanceledExceptionType)
                .ToImmutableArray();

            if (!catches.Any())
            {
                // No catch blocks of interest
                return;
            }

            if (!(GetFirstCancellationTokenExpressionOrDefault(context, tryStatement) is string cancellationTokenExpression))
            {
                return;
            }

            foreach (var (catchClause, catchType) in catches)
            {
                if (catchType == OperationCanceledExceptionType)
                {
                    if (!HasFilterWhichGetsIsCancellationRequestedFromExpression(catchClause, cancellationTokenExpression))
                    {
                        context.ReportDiagnostic(DiagnosticDescriptors.ImproperTryCatchOperationCanceled, catchClause.CatchKeyword);
                    }

                    return;
                }

                if (catchType == ExceptionType)
                {
                    if (HasFilterIncludingExpression(catchClause, cancellationTokenExpression))
                    {
                        if (HasFilterIncludingCaughtException(catchClause))
                        {
                            return;
                        }
                    }

                    context.ReportDiagnostic(DiagnosticDescriptors.ImproperTryCatchSystemException, catchClause.CatchKeyword);
                    return;
                }
            }
        }

        static bool HasFilterWhichGetsIsCancellationRequestedFromExpression(CatchClauseSyntax catchClause, string expression)
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

            return memberAccess.Expression.ToString() == expression;
        }

        static bool HasFilterIncludingExpression(CatchClauseSyntax catchClause, string expression)
        {
            if (catchClause.Filter == null)
            {
                return false;
            }

            var tokens = catchClause.Filter.DescendantNodes()
                .Where(node => node.ToString() == expression);

            return tokens.Any();
        }

        static bool HasFilterIncludingCaughtException(CatchClauseSyntax catchClause)
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

        static string GetFirstCancellationTokenExpressionOrDefault(SyntaxNodeAnalysisContext context, TryStatementSyntax tryStatement)
        {
            // Because we are examining all descendants, this may result in false positives.
            // For example, a nested try block may contain cancellable invocations and
            // a related catch block may swallow OperationCanceledException.
            // Or, an anonymous delegate may contain cancellable invocations but
            // may not actually be executed in the try block.
            // However, these are edge cases and would be complicated to analyze.
            // In these cases, either the fix can be redundantly applied, or the analyzer can be suppressed.
            var tryBlockCalls = tryStatement.Block.DescendantNodes().OfType<InvocationExpressionSyntax>();

            var distinctTokenExpressions = GetCancellationTokenExpressions(tryBlockCalls, context)
                .Distinct()
                .ToImmutableArray();

            if (distinctTokenExpressions.Length > 1)
            {
                context.ReportDiagnostic(DiagnosticDescriptors.MultipleCancellationTokensInATry, tryStatement.TryKeyword);
            }

            return distinctTokenExpressions.FirstOrDefault();
        }

        static IEnumerable<string> GetCancellationTokenExpressions(IEnumerable<InvocationExpressionSyntax> calls, SyntaxNodeAnalysisContext context)
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
                            continue;
                        }
                    }
                }

                var argExpressions = call.ArgumentList.Arguments
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

                foreach (var expr in argExpressions)
                {
                    yield return expr;
                }
            }
        }

        static string GetCatchType(CatchClauseSyntax catchClause)
        {
            var catchType = catchClause.Declaration?.Type.ToString();

            // if catchClause.Declaration is null, that means:
            //   catch
            //   {
            //   }
            // assume "Exception" and "OperationCanceledException" refer to the System types
            switch (catchType)
            {
                case null:
                case "Exception":
                    return ExceptionType;
                case "OperationCanceledException":
                    return OperationCanceledExceptionType;
                default:
                    return catchType;
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
