namespace Particular.CodeRules.Cancellation
{
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Particular.CodeRules.Extensions;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MethodFuncParameterAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.MethodFuncParameterCancellationTokenMisplaced,
            DiagnosticDescriptors.MethodFuncParameterMixedCancellation,
            DiagnosticDescriptors.MethodFuncParameterMultipleCancellableContexts,
            DiagnosticDescriptors.MethodFuncParameterMultipleCancellationTokens,
            DiagnosticDescriptors.MethodFuncParameterTaskReturnTypeNoCancellation);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(
                Analyze,
                SyntaxKind.ConstructorDeclaration,
                SyntaxKind.DelegateDeclaration,
                SyntaxKind.MethodDeclaration);
        }

        static void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is MemberDeclarationSyntax member))
            {
                return;
            }

            if (!(context.SemanticModel.GetMethod(member, context.CancellationToken, out _) is IMethodSymbol method))
            {
                return;
            }

            Analyze(context, method);
        }

        static void Analyze(SyntaxNodeAnalysisContext context, IMethodSymbol method)
        {
            // cheapest checks first
            if (method.IsOverride)
            {
                return;
            }

            if (method.MethodKind == MethodKind.ExplicitInterfaceImplementation)
            {
                return;
            }

            foreach (var param in method.Parameters)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                Analyze(context, param);
            }
        }

        static void Analyze(SyntaxNodeAnalysisContext context, IParameterSymbol param)
        {
            if (!(param.Type is INamedTypeSymbol type))
            {
                return;
            }

            // cheapest checks first
            if (!type.IsFunc())
            {
                return;
            }

            var inputs = type.TypeArguments.Take(type.TypeArguments.Length - 1).ToList();
            var tokens = inputs.Where(input => input.IsCancellationToken()).Take(2).ToList();
            var contextCount = inputs.Where(input => input.IsCancellableContext()).Take(2).Count();

            if (type.TypeArguments.Last().IsTask() && tokens.Count == 0 && contextCount == 0)
            {
                context.ReportDiagnostic(DiagnosticDescriptors.MethodFuncParameterTaskReturnTypeNoCancellation, param);
            }

            if (tokens.Count > 1)
            {
                context.ReportDiagnostic(DiagnosticDescriptors.MethodFuncParameterMultipleCancellationTokens, param);
            }

            if (contextCount > 1)
            {
                context.ReportDiagnostic(DiagnosticDescriptors.MethodFuncParameterMultipleCancellableContexts, param);
            }

            if (tokens.Count > 0 && contextCount > 0)
            {
                context.ReportDiagnostic(DiagnosticDescriptors.MethodFuncParameterMixedCancellation, param);
            }

            if (tokens.Count > 0 && inputs.IndexOf(tokens[0]) < inputs.FindLastIndex(input => !input.IsCancellationToken()))
            {
                context.ReportDiagnostic(DiagnosticDescriptors.MethodFuncParameterCancellationTokenMisplaced, param);
            }
        }
    }
}
