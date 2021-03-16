namespace Particular.CodeRules.Tests.Cancellation
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Particular.CodeRules.Cancellation;
    using Particular.CodeRules.Tests.Helpers;
    using Xunit;
    using Xunit.Abstractions;
    using Data = System.Collections.Generic.IEnumerable<object[]>;

    public class DelegateParametersAnalyzerTests : AnalyzerTestFixture<DelegateParametersAnalyzer>
    {
        static readonly string @delegate =
@"class MyClass
{{
    delegate void MyDelegate({0});
}}";

        public DelegateParametersAnalyzerTests(ITestOutputHelper output) : base(output) { }

        public static Data SadParams => new List<string>
        {
            "CancellationToken [|cancellationToken|], object foo",
            "CancellationToken [|cancellationToken1|], CancellationToken [|cancellationToken2|], object foo",
            "CancellationToken [|cancellationToken1|], object foo, CancellationToken cancellationToken2",
        }.ToData();

        public static Data HappyParams => new List<string>
        {
            "",
            "CancellationToken cancellationToken",
            "object foo, CancellationToken cancellationToken",
            "object foo, CancellationToken cancellationToken1, CancellationToken cancellationToken2",
            "CancellationToken cancellationToken, object foo = null",
            "CancellationToken cancellationToken1, CancellationToken cancellationToken2, object foo = null",
            "CancellationToken cancellationToken1, CancellationToken cancellationToken2, params object[] foos",
        }.ToData();

        [Theory]
        [MemberData(nameof(SadParams))]
        public Task Sad(string @params) => Assert(GetCode(@params), DiagnosticIds.DelegateCancellationTokenMisplaced);

        [Theory]
        [MemberData(nameof(HappyParams))]
        public Task Happy(string @params) => Assert(GetCode(@params));

        static string GetCode(string @params) => string.Format(@delegate, @params);
    }
}
