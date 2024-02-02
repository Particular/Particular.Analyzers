namespace Particular.Analyzers
{
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Particular.Analyzers.Extensions;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LoggingAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.StructuredLoggingWithRepeatedToken);

        static readonly ImmutableHashSet<string> wellKnownNsbLoggingMethodsWithFormat = new[]
        {
            "DebugFormat",
            "InfoFormat",
            "WarnFormat",
            "ErrorFormat",
            "FatalFormat",
        }
        .ToImmutableHashSet();

        static readonly ImmutableHashSet<string> wellKnownMsLoggingMethodsWithFormat = new[]
        {
            "Log",
            "LogTrace",
            "LogDebug",
            "LogInformation",
            "LogWarning",
            "LogError",
            "LogCritical",
        }
        .ToImmutableHashSet();

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression);
        }

        static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is InvocationExpressionSyntax invocation))
            {
                return;
            }

            if (!(invocation.Expression is MemberAccessExpressionSyntax memberAccess))
            {
                return;
            }

            if (!(memberAccess.Name is IdentifierNameSyntax identifierName))
            {
                return;
            }

            if (wellKnownNsbLoggingMethodsWithFormat.Contains(identifierName.Identifier.Text))
            {
                if (Analyze(context, invocation, "NServiceBus.Logging.ILog"))
                {
                    return;
                }
            }

            if (wellKnownMsLoggingMethodsWithFormat.Contains(identifierName.Identifier.Text))
            {
                if (Analyze(context, invocation, "Microsoft.Extensions.Logging.ILogger"))
                {
                    return;
                }
            }
        }

        static bool Analyze(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocationSyntax, string loggerFullName)
        {
            var loggerSymbol = context.SemanticModel.GetSymbolInfo(invocationSyntax, context.CancellationToken).Symbol;

            if (!(loggerSymbol.GetMethodOrDefault() is IMethodSymbol method))
            {
                return false;
            }

            if (method.ReceiverType.ToString() != loggerFullName)
            {
                return false;
            }

            int formatIndex = -1;
            int argsIndex = -1;

            for (int i = 0; i < method.Parameters.Length; i++)
            {
                var p = method.Parameters[i];
                if (formatIndex < 0)
                {
                    if (p.Name == "message" || p.Name == "format")
                    {
                        var typeString = p.Type.ToString();
                        if (typeString == "string" || typeString == "string?")
                        {
                            formatIndex = i;
                        }
                    }
                }
                else if (argsIndex < 0)
                {
                    if (p.Type is IArrayTypeSymbol arrayType)
                    {
                        var elementType = arrayType.ElementType.ToString();
                        if (elementType == "object" || elementType == "object?")
                        {
                            argsIndex = i;
                            break;
                        }
                    }
                }
            }

            if (formatIndex < 0 || argsIndex < 0)
            {
                return false;
            }

            var argumentSyntaxes = invocationSyntax.ArgumentList.Arguments;

            // Try to find the value of the format argument
            if (formatIndex >= argumentSyntaxes.Count || !(argumentSyntaxes[formatIndex].Expression is LiteralExpressionSyntax literalExpression && literalExpression.IsKind(SyntaxKind.StringLiteralExpression)))
            {
                return false;
            }

            var formatString = literalExpression.ToString();
            var argMatches = FormatExpressionArgumentRegex.Matches(formatString);
            if (argMatches.Count <= 1)
            {
                return false;
            }

            var allKeys = argMatches.OfType<Match>().Select(m => m.Value).ToImmutableArray();
            var uniqueKeyCount = allKeys.Distinct().Count();
            if (uniqueKeyCount == argMatches.Count)
            {
                return false;
            }

            // Now find out which one (the first one, anyway) that's duplicated
            var firstDupe = allKeys.GroupBy(key => key).OrderByDescending(g => g.Count()).First().Key;

            context.ReportDiagnostic(DiagnosticDescriptors.StructuredLoggingWithRepeatedToken, invocationSyntax, firstDupe);
            return true;
        }

        static readonly Regex FormatExpressionArgumentRegex = new Regex(@"\{\w\}", RegexOptions.Compiled);
    }
}
