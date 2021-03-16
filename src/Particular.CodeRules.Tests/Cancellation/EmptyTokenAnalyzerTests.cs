namespace Particular.CodeRules.Tests.Cancellation
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Particular.CodeRules.Cancellation;
    using Particular.CodeRules.Tests.Helpers;
    using Xunit;
    using Xunit.Abstractions;
    using Data = System.Collections.Generic.IEnumerable<object[]>;

    public class EmptyTokenAnalyzerTests : AnalyzerTestFixture<EmptyTokenAnalyzer>
    {
        static readonly string invoke =
@"class MyClass
{{
    void MyMethod(CancellationToken cancellationToken) => Task.Delay(1, [|{0}|]);
}}";

        public EmptyTokenAnalyzerTests(ITestOutputHelper output) : base(output) { }

        public static Data SadArgs => new List<(string, string)>
        {
            ("default", DiagnosticIds.EmptyCancellationTokenDefaultLiteral),
            ("default(CancellationToken)", DiagnosticIds.EmptyCancellationTokenDefaultOperator),
            ("CancellationToken.None", DiagnosticIds.EmptyCancellationTokenNone),
        }.ToData();

        public static Data HappyArgs => new List<string> { "cancellationToken", "new CancellationToken()", }.ToData();

        [Theory]
        [MemberData(nameof(SadArgs))]
        public Task Sad(string arg, string diagnosticId) => Assert(GetCode(arg), diagnosticId);

        [Theory]
        [MemberData(nameof(HappyArgs))]
        public Task Happy(string arg) => Assert(GetCode(arg));

        static string GetCode(string arg) => string.Format(invoke, arg);
    }
}
