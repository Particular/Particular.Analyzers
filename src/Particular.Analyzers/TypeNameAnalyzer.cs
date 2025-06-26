namespace Particular.Analyzers
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Particular.Analyzers.Extensions;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TypeNameAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.NonInterfaceTypePrefixedWithI];

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
                SyntaxKind.DelegateDeclaration);
        }

        static void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not MemberDeclarationSyntax type)
            {
                return;
            }

            if (!TryGetIdentifier(type, out var identifier))
            {
                return;
            }

            var name = identifier.Text;

            if (name.Length < 2)
            {
                return;
            }

            if (name[0] != 'I')
            {
                return;
            }

            if (!char.IsUpper(name[1]))
            {
                return;
            }

            context.ReportDiagnostic(DiagnosticDescriptors.NonInterfaceTypePrefixedWithI, identifier, name);
        }

        static bool TryGetIdentifier(MemberDeclarationSyntax member, out SyntaxToken identifier)
        {
            identifier = default;

            switch (member)
            {
                case BaseTypeDeclarationSyntax type:
                    identifier = type.Identifier;
                    return true;
                case DelegateDeclarationSyntax @delegate:
                    identifier = @delegate.Identifier;
                    return true;
                default:
                    return false;
            }
        }
    }
}
