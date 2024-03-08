namespace Particular.Analyzers
{
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AsyncVoidAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.AsyncVoid);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.LocalFunctionStatement);
        }

        void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is MethodDeclarationSyntax method)
            {
                Analyze(context, method.Identifier, method.ReturnType, method.Modifiers);
            }
            else if (context.Node is LocalFunctionStatementSyntax localFn)
            {
                Analyze(context, localFn.Identifier, localFn.ReturnType, localFn.Modifiers);
            }
        }

        static void Analyze(SyntaxNodeAnalysisContext context, SyntaxToken identifier, TypeSyntax returnType, SyntaxTokenList modifiers)
        {
            if (returnType?.ToString() == "void" && modifiers.Any(token => token.IsKind(SyntaxKind.AsyncKeyword)))
            {
                var diagnostic = Diagnostic.Create(DiagnosticDescriptors.AsyncVoid, identifier.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
