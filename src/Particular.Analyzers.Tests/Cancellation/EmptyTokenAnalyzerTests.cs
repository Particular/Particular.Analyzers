namespace Particular.Analyzers.Tests.Cancellation
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Particular.Analyzers.Cancellation;
    using Particular.Analyzers.Tests.Helpers;
    using Data = System.Collections.Generic.IEnumerable<object[]>;

    public class EmptyTokenAnalyzerTests : AnalyzerTestFixture<EmptyTokenAnalyzer>
    {
        static readonly string invoke =
@"class MyClass
{{
    void MyMethod(CancellationToken cancellationToken) => Task.Delay(1, [|{0}|]);
}}";

        public static readonly Data SadArgs = new List<(string, string)>
        {
            ("default", DiagnosticIds.EmptyCancellationTokenDefaultLiteral),
            ("default(CancellationToken)", DiagnosticIds.EmptyCancellationTokenDefaultOperator),
        }.ToData();

        public static readonly Data HappyArgs = new List<string> { "cancellationToken", "new CancellationToken()", "CancellationToken.None" }.ToData();

        [Test]
        [TestCaseSource(nameof(SadArgs))]
        public Task Sad(string arg, string diagnosticId) => Assert(GetCode(arg), diagnosticId);

        [Test]
        [TestCaseSource(nameof(HappyArgs))]
        public Task Happy(string arg) => Assert(GetCode(arg));

        static string GetCode(string arg) => string.Format(invoke, arg);
    }
}
