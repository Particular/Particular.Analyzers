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
                SyntaxKind.EnumDeclaration,
                SyntaxKind.DelegateDeclaration
                );
        }

        static void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is MemberDeclarationSyntax type))
            {
                return;
            }
            var identifier = GetIdentifierOrDefault(type);

            if (identifier == default)
            {
                return;
            }

            var name = identifier.Text;

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

            context.ReportDiagnostic(DiagnosticDescriptors.NonInterfaceTypePrefixedWithI, identifier, name);
        }

        static SyntaxToken GetIdentifierOrDefault(MemberDeclarationSyntax member)
        {
            switch (member)
            {
                case BaseTypeDeclarationSyntax type:
                    return type.Identifier;
                case DelegateDeclarationSyntax @delegate:
                    return @delegate.Identifier;
                default:
                    return default;
            }
        }
    }
}
