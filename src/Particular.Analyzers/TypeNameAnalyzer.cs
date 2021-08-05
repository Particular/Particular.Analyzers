namespace Particular.Analyzers
{
    using System;
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Particular.Analyzers.Extensions;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TypeNameAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.NonInterfaceTypePrefixedWithI);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(
                Analyze,
                SyntaxKind.ClassDeclaration,
                SyntaxKind.RecordDeclaration,
                SyntaxKind.StructDeclaration,
                SyntaxKind.EnumDeclaration
                //TODO SyntaxKind.DelegateDeclaration
                );
        }

        static void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is BaseTypeDeclarationSyntax type))
            {
                return;
            }
            var name = type.Identifier.Text;

            if (name.Length == 1)
            {
                return;
            }

            if (!name.StartsWith("I", StringComparison.Ordinal))
            {
                return;
            }

            if (!char.IsUpper(name[1]))
            {
                return;
            }

            context.ReportDiagnostic(DiagnosticDescriptors.NonInterfaceTypePrefixedWithI, type.Identifier, name);
        }
    }
}
