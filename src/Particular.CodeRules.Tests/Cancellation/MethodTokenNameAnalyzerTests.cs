namespace Particular.CodeRules.Tests.Cancellation
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Particular.CodeRules.Cancellation;
    using Particular.CodeRules.Tests.Helpers;
    using Xunit;
    using Xunit.Abstractions;
    using Data = System.Collections.Generic.IEnumerable<object[]>;

    public class MethodTokenNameAnalyzerTests : AnalyzerTestFixture<MethodTokenNameAnalyzer>
    {
        static readonly string method =
@"class MyClass<T> where T : CancellableContext
{{
    void MyMethod({0}) {{ }}
}}";

        static readonly string constructor =
@"class MyClass<T> where T : CancellableContext
{{
    MyClass({0}) {{ }}
}}";

        static readonly string @delegate =
@"class MyClass<T> where T : CancellableContext
{{
    delegate void MyDelegate({0});
}}";

        static readonly string @override =
@"class MyBase<T> where T : CancellableContext
{{
    protected virtual void MyMethod({0}) {{ }}
}}

class MyClass<T> : MyBase<T> where T : CancellableContext
{{
    protected override void MyMethod({0}) {{ }}
}}";

        static readonly string @explicit =
@"interface IMyInterface<T> where T : CancellableContext
{{
    void MyMethod({0});
}}

class MyClass<T> : IMyInterface<T> where T : CancellableContext
{{
    void IMyInterface<T>.MyMethod({0}) {{ }}
}}";

        public MethodTokenNameAnalyzerTests(ITestOutputHelper output) : base(output) { }

        public static Data SadParams => new List<string>
        {
            "CancellationToken [|token|]",
            "object foo, CancellationToken [|token|]",
        }.ToData();

        public static Data HappyParams => new List<string>
        {
            "CancellationToken cancellationToken",
            "object foo, CancellationToken cancellationToken",
            "CancellationToken token1, CancellationToken token2",
            "object foo, CancellationToken token1, CancellationToken token2",
        }.ToData();

        [Theory]
        [MemberData(nameof(SadParams))]
        public Task SadMethods(string @params) => Assert(GetCode(method, @params), DiagnosticIds.MethodCancellationTokenMisnamed);

        [Theory]
        [MemberData(nameof(HappyParams))]
        public Task HappyMethods(string @params) => Assert(GetCode(method, @params));

        [Theory]
        [MemberData(nameof(SadParams))]
        public Task SadConstructors(string @params) => Assert(GetCode(constructor, @params), DiagnosticIds.MethodCancellationTokenMisnamed);

        [Theory]
        [MemberData(nameof(HappyParams))]
        public Task HappyConstructors(string @params) => Assert(GetCode(constructor, @params));

        [Theory]
        [MemberData(nameof(SadParams))]
        public Task SadDelegates(string @params) => Assert(GetCode(@delegate, @params), DiagnosticIds.MethodCancellationTokenMisnamed);

        [Theory]
        [MemberData(nameof(HappyParams))]
        public Task HappyDelegates(string @params) => Assert(GetCode(@delegate, @params));

        [Theory]
        [MemberData(nameof(SadParams))]
        public Task SadOverrides(string @params) => Assert(GetCode(@override, @params), DiagnosticIds.MethodCancellationTokenMisnamed);

        [Theory]
        [MemberData(nameof(HappyParams))]
        public Task HappyOverrides(string @params) => Assert(GetCode(@override, @params));

        [Theory]
        [MemberData(nameof(SadParams))]
        public Task SadExplicits(string @params) => Assert(GetCode(@explicit, @params), DiagnosticIds.MethodCancellationTokenMisnamed);

        [Theory]
        [MemberData(nameof(HappyParams))]
        public Task HappyExplicits(string @params) => Assert(GetCode(@explicit, @params));

        static string GetCode(string template, string @params) => string.Format(template, @params);
    }
}
