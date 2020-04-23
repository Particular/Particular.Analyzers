namespace Particular.CodeRules.ConfigureAwait
{
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    /// <summary>
    ///     The code fix for any missing ConfigureAwaits
    /// </summary>
    /// <seealso cref="Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider" />
    [ExportCodeFixProvider(DiagnosticIds.UseConfigureAwait, LanguageNames.CSharp)]
    public class ConfigureAwaitCodeFix : CodeFixProvider
    {
        /// <summary>
        ///     Gets the fixable diagnostic ids.
        /// </summary>
        /// <value>
        ///     The fixable diagnostic ids.
        /// </value>
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticIds.UseConfigureAwait);

        /// <summary>
        /// Gets the fix all provider type
        /// </summary>
        /// <returns>The type of provider applicable</returns>
        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        /// <summary>
        ///     Registers the code fixes asynchronous.
        /// </summary>
        /// <param name="context">The <paramref name="context" /> .</param>
        /// <returns>
        /// </returns>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var declaration = root.FindToken(diagnosticSpan.Start)
                .Parent
                .FirstAncestorOrSelf<AwaitExpressionSyntax>();

            context.RegisterCodeFix(
                CodeAction.Create("Add ConfigureAwait(true)",
                    cancellationToken => AddConfigureAwait(context.Document, declaration, true, cancellationToken),
                    "Add ConfigureAwait(true)"), diagnostic);

            context.RegisterCodeFix(
                CodeAction.Create("Add ConfigureAwait(false)",
                    cancellationToken => AddConfigureAwait(context.Document, declaration, false, cancellationToken),
                    "Add ConfigureAwait(false)"), diagnostic);
        }

        private Task<Document> AddConfigureAwait(Document document, AwaitExpressionSyntax awaitSyntax, bool value, CancellationToken cancellationToken)
        {
            var oldExpression = awaitSyntax.Expression;
            var newExpression =
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, oldExpression,
                        SyntaxFactory.IdentifierName("ConfigureAwait")),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                SyntaxFactory.LiteralExpression(
                                    value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression)))));

            return ReplaceAsync(document, oldExpression, newExpression, cancellationToken);
        }

        private static async Task<Document> ReplaceAsync(Document document, SyntaxNode oldSyntax, SyntaxNode newSyntax, CancellationToken cancellationToken)
        {
            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = oldRoot.ReplaceNode(oldSyntax, newSyntax);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}