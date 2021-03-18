namespace Particular.CodeRules.Tests
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;

    public class CSharpAnalyzerTestFixture<TAnalyzer> : AnalyzerTestFixture<TAnalyzer> where TAnalyzer : DiagnosticAnalyzer, new()
    {
        protected override string LanguageName => LanguageNames.CSharp;
    }
}
