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
    public class TaskReturningMethodAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.TaskReturningMethodNoCancellation);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(
                Analyze,
                SyntaxKind.DelegateDeclaration,
                SyntaxKind.MethodDeclaration);
        }

        static void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not MemberDeclarationSyntax member)
            {
                return;
            }

            if (context.SemanticModel.GetMethod(member, context.CancellationToken, out var declaredSymbol) is not IMethodSymbol method || declaredSymbol is null)
            {
                return;
            }

            Analyze(context, method, declaredSymbol);
        }

        static void Analyze(SyntaxNodeAnalysisContext context, IMethodSymbol method, ISymbol declaredSymbol)
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

            if (method.IsEntryPoint())
            {
                return;
            }

            if (!method.ReturnType.IsTask())
            {
                return;
            }

            if (method.IsFromAsyncDisposable())
            {
                return;
            }

            if (method.ContainingType?.IsCancellableContext() ?? false)
            {
                return;
            }

            if (method.IsTest())
            {
                return;
            }

            if (!method.Parameters.Any(param => param.Type.IsCancellableContext() || param.Type.IsCancellationToken()))
            {
                context.ReportDiagnostic(DiagnosticDescriptors.TaskReturningMethodNoCancellation, declaredSymbol);
            }
        }
    }
}
