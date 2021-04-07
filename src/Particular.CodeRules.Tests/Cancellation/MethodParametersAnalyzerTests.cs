﻿namespace Particular.CodeRules.Tests.Cancellation
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Particular.CodeRules.Cancellation;
    using Particular.CodeRules.Tests.Helpers;
    using Xunit;
    using Xunit.Abstractions;
    using Data = System.Collections.Generic.IEnumerable<object[]>;

    public class MethodParametersAnalyzerTests : AnalyzerTestFixture<MethodParametersAnalyzer>
    {
        static readonly string method =
@"class MyClass<T> where T : CancellableContext
{{
    void [|MyMethod|]({0}) {{ }}
}}";

        static readonly string constructor =
@"class MyClass<T> where T : CancellableContext
{{
    [|MyClass|]({0}) {{ }}
}}";

        static readonly string @delegate =
@"class MyClass<T> where T : CancellableContext
{{
    delegate void [|MyDelegate|]({0});
}}";

        static readonly string @override =
@"class MyBase<T> where T : CancellableContext
{{
#pragma warning disable
    protected virtual void MyMethod({0}) {{ }}
#pragma warning restore
}}

class MyClass<T> : MyBase<T> where T : CancellableContext
{{
    protected override void MyMethod({0}) {{ }}
}}";

        static readonly string @explicit =
@"interface IMyInterface<T> where T : CancellableContext
{{
#pragma warning disable
    void MyMethod({0});
#pragma warning restore
}}

class MyClass<T> : IMyInterface<T> where T : CancellableContext
{{
    void IMyInterface<T>.MyMethod({0}) {{ }}
}}";

        public MethodParametersAnalyzerTests(ITestOutputHelper output) : base(output) { }

        public static readonly Data SadParams = new List<(string, string[])>
        {
            ("ICancellableContext foo, CancellationToken cancellationToken", new[] { DiagnosticIds.MethodMixedCancellation }),
            ("CancellableContext foo, CancellationToken cancellationToken", new[] { DiagnosticIds.MethodMixedCancellation }),
            ("T foo, CancellationToken cancellationToken", new[] { DiagnosticIds.MethodMixedCancellation }),

            ("ICancellableContext foo, ICancellableContext bar", new[] { DiagnosticIds.MethodMultipleCancellableContexts }),
            ("CancellableContext foo, CancellableContext bar", new[] { DiagnosticIds.MethodMultipleCancellableContexts }),
            ("T foo, T bar", new[] { DiagnosticIds.MethodMultipleCancellableContexts }),
            ("T foo, T bar, T baz", new[] { DiagnosticIds.MethodMultipleCancellableContexts }),

            ("CancellationToken foo, CancellationToken bar", new[] { DiagnosticIds.MethodMultipleCancellationTokens }),
            ("CancellationToken foo, CancellationToken bar, CancellationToken baz", new[] { DiagnosticIds.MethodMultipleCancellationTokens }),

            (
                "T foo, T bar, CancellationToken token1, CancellationToken token2",
                new[]
                {
                    DiagnosticIds.MethodMixedCancellation,
                    DiagnosticIds.MethodMultipleCancellableContexts,
                    DiagnosticIds.MethodMultipleCancellationTokens
                }),
        }.ToData();

        public static readonly Data HappyParams = new List<string>
        {
            "CancellationToken cancellationToken",
            "object foo, CancellationToken cancellationToken",
            "ICancellableContext foo",
            "ICancellableContext foo, object bar",
            "CancellableContext foo",
            "CancellableContext foo, object bar",
            "T foo",
            "T foo, object bar",
        }.ToData();

        [Theory]
        [MemberData(nameof(SadParams))]
        public Task SadMethods(string @params, params string[] diagnosticIds) => Assert(GetCode(method, @params), diagnosticIds);

        [Theory]
        [MemberData(nameof(HappyParams))]
        public Task HappyMethods(string @params) => Assert(GetCode(method, @params));

        [Theory]
        [MemberData(nameof(SadParams))]
        public Task SadConstructors(string @params, params string[] diagnosticIds) => Assert(GetCode(constructor, @params), diagnosticIds);

        [Theory]
        [MemberData(nameof(HappyParams))]
        public Task HappyConstructors(string @params) => Assert(GetCode(constructor, @params));

        [Theory]
        [MemberData(nameof(SadParams))]
        public Task SadDelegates(string @params, params string[] diagnosticIds) => Assert(GetCode(@delegate, @params), diagnosticIds);

        [Theory]
        [MemberData(nameof(HappyParams))]
        public Task HappyDelegates(string @params) => Assert(GetCode(@delegate, @params));

        [Theory]
        [MemberData(nameof(SadParams))]
        [MemberData(nameof(HappyParams))]
        public Task HappyOverrides(string @params, params string[] diagnosticIds) => Assert(GetCode(@override, @params), diagnosticIds);

        [Theory]
        [MemberData(nameof(SadParams))]
        [MemberData(nameof(HappyParams))]
        public Task HappyExplicits(string @params, params string[] diagnosticIds) => Assert(GetCode(@explicit, @params), diagnosticIds);

        static string GetCode(string template, string @params) => string.Format(template, @params);
    }
}
