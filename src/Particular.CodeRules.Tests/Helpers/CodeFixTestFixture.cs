﻿using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Particular.CodeRules.Tests
{
    public class CSharpCodeFixTestFixture<TCodeFix> : CodeFixTestFixture where TCodeFix : CodeFixProvider, new()
    {
        protected override string LanguageName => LanguageNames.CSharp;

        protected override CodeFixProvider CreateProvider() => new TCodeFix();
    }

    public abstract class CodeFixTestFixture : BaseTestFixture
    {
        protected abstract CodeFixProvider CreateProvider();

        protected Task TestCodeFix(string markupCode, string expected, DiagnosticDescriptor descriptor, int count = 1, int index = 0)
        {
            Assert.True(TestHelpers.TryGetDocumentAndSpanFromMarkup(markupCode, LanguageName, out var document, out var span), "No markup detected in test code.");

            return TestCodeFix(document, span, expected, descriptor, count, index);
        }

        protected async Task TestCodeFix(Document document, TextSpan span, string expected, DiagnosticDescriptor descriptor, int count = 1, int index = 0)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            var codeFixes = await GetCodeFixes(document, span, descriptor).ConfigureAwait(false);
            Assert.Equal(count, codeFixes.Length);

            await CodeAction(codeFixes[index], document, expected).ConfigureAwait(false);
        }

        private async Task<ImmutableArray<CodeAction>> GetCodeFixes(Document document, TextSpan span, DiagnosticDescriptor descriptor)
        {
            var builder = ImmutableArray.CreateBuilder<CodeAction>();

            void registerCodeFix(CodeAction a, ImmutableArray<Diagnostic> _) => builder.Add(a);

            var tree = await document.GetSyntaxTreeAsync(CancellationToken.None).ConfigureAwait(false);
            var diagnostic = Diagnostic.Create(descriptor, Location.Create(tree, span));
            var context = new CodeFixContext(document, diagnostic, registerCodeFix, CancellationToken.None);

            var provider = CreateProvider();
            await provider.RegisterCodeFixesAsync(context).ConfigureAwait(false);

            return builder.ToImmutable();
        }

        private static async Task CodeAction(CodeAction codeAction, Document document, string expectedCode)
        {
            var operations = await codeAction.GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.Single(operations);

            var operation = operations[0];
            var workspace = document.Project.Solution.Workspace;
            operation.Apply(workspace, CancellationToken.None);

            var newDocument = workspace.CurrentSolution.GetDocument(document.Id);

            var sourceText = await newDocument.GetTextAsync(CancellationToken.None).ConfigureAwait(false);
            var text = sourceText.ToString();

            Assert.Equal(expectedCode, text);
        }
    }
}