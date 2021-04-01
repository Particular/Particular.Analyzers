﻿namespace Particular.CodeRules.Tests.Cancellation
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Particular.CodeRules.Cancellation;
    using Particular.CodeRules.Tests.Helpers;
    using Xunit;
    using Xunit.Abstractions;
    using Data = System.Collections.Generic.IEnumerable<object[]>;

    public class MethodFuncParameterAnalyzerTests : AnalyzerTestFixture<MethodFuncParameterAnalyzer>
    {
        static readonly string method =
@"class MyClass<T> where T : CancellableContext
{{
    void MyMethod({0} [|foo|]) {{ }}
}}";

        static readonly string constructor =
@"class MyClass<T> where T : CancellableContext
{{
    MyClass({0} [|foo|]) {{ }}
}}";

        static readonly string @delegate =
@"class MyClass<T> where T : CancellableContext
{{
    delegate void MyDelegate({0} [|foo|]);
}}";

        static readonly string @override =
@"class MyBase<T> where T : CancellableContext
{{
#pragma warning disable
    protected virtual void MyMethod({0} foo) {{ }}
#pragma warning restore
}}

class MyClass<T> : MyBase<T> where T : CancellableContext
{{
    protected override void MyMethod({0} foo) {{ }}
}}";

        static readonly string @explicit =
@"interface IMyInterface<T> where T : CancellableContext
{{
#pragma warning disable
    void MyMethod({0} foo);
#pragma warning restore
}}

class MyClass<T> : IMyInterface<T> where T : CancellableContext
{{
    void IMyInterface<T>.MyMethod({0} foo) {{ }}
}}";

        public MethodFuncParameterAnalyzerTests(ITestOutputHelper output) : base(output) { }

        public static Data SadNames => new List<(string, string[])>
        {
            ("Func<CancellableContext, CancellationToken, object>", new[] { DiagnosticIds.MethodFuncParameterMixedCancellation }),
            ("Func<ICancellableContext, CancellationToken, object>", new[] { DiagnosticIds.MethodFuncParameterMixedCancellation }),
            ("Func<T, CancellationToken, object>", new[] { DiagnosticIds.MethodFuncParameterMixedCancellation }),

            ("Func<CancellationToken, object, object>", new[] { DiagnosticIds.MethodFuncParameterCancellationTokenMisplaced }),
            ("Func<CancellationToken, object, CancellationToken, object>", new[] { DiagnosticIds.MethodFuncParameterCancellationTokenMisplaced, DiagnosticIds.MethodFuncParameterMultipleCancellationTokens }),
            ("Func<object, CancellationToken, object, Task>", new[] { DiagnosticIds.MethodFuncParameterCancellationTokenMisplaced }),

            ("Func<CancellableContext, CancellableContext, object>", new[] { DiagnosticIds.MethodFuncParameterMultipleCancellableContexts }),
            ("Func<ICancellableContext, ICancellableContext, object>", new[] { DiagnosticIds.MethodFuncParameterMultipleCancellableContexts }),
            ("Func<T, T, object>", new[] { DiagnosticIds.MethodFuncParameterMultipleCancellableContexts }),

            ("Func<CancellationToken, CancellationToken, object>", new[] { DiagnosticIds.MethodFuncParameterMultipleCancellationTokens }),

            ("Func<Task>", new[] { DiagnosticIds.MethodFuncParameterTaskReturnTypeNoCancellation }),
            ("Func<Task<object>>", new[] { DiagnosticIds.MethodFuncParameterTaskReturnTypeNoCancellation }),
#if NETCOREAPP
            ("Func<ValueTask>", new[] { DiagnosticIds.MethodFuncParameterTaskReturnTypeNoCancellation }),
            ("Func<ValueTask<object>>", new[] { DiagnosticIds.MethodFuncParameterTaskReturnTypeNoCancellation }),
#endif
            ("Func<object, Task>", new[] { DiagnosticIds.MethodFuncParameterTaskReturnTypeNoCancellation }),
            ("Func<object, Task<object>>", new[] { DiagnosticIds.MethodFuncParameterTaskReturnTypeNoCancellation }),
#if NETCOREAPP
            ("Func<object, ValueTask>", new[] { DiagnosticIds.MethodFuncParameterTaskReturnTypeNoCancellation }),
            ("Func<object, ValueTask<object>>", new[] { DiagnosticIds.MethodFuncParameterTaskReturnTypeNoCancellation }),
#endif
        }.ToData();

        public static Data HappyNames => new List<string>
        {
            "object",
            "Action",
            "Action<object>",
            "Func<object>",
            "IMessage",

            "Func<CancellationToken, Task>",
            "Func<CancellationToken, Task<object>>",
#if NETCOREAPP
            "Func<CancellationToken, ValueTask>",
            "Func<CancellationToken, ValueTask<object>>",
#endif

            "Func<ICancellableContext, Task>",
            "Func<CancellableContext, Task>",
            "Func<T, Task>",
        }.ToData();

        [Theory]
        [MemberData(nameof(SadNames))]
        public Task SadMethods(string name, params string[] diagnosticIds) => Assert(GetCode(method, name), diagnosticIds);

        [Theory]
        [MemberData(nameof(HappyNames))]
        public Task HappyMethods(string name) => Assert(GetCode(method, name));

        [Theory]
        [MemberData(nameof(SadNames))]
        public Task SadConstructors(string name, params string[] diagnosticIds) => Assert(GetCode(constructor, name), diagnosticIds);

        [Theory]
        [MemberData(nameof(HappyNames))]
        public Task HappyConstructors(string name) => Assert(GetCode(constructor, name));

        [Theory]
        [MemberData(nameof(SadNames))]
        public Task SadDelegates(string name, params string[] diagnosticIds) => Assert(GetCode(@delegate, name), diagnosticIds);

        [Theory]
        [MemberData(nameof(HappyNames))]
        public Task HappyDelegates(string name) => Assert(GetCode(@delegate, name));

        [Theory]
        [MemberData(nameof(SadNames))]
        [MemberData(nameof(HappyNames))]
        public Task HappyOverrides(string name, params string[] diagnosticIds) => Assert(GetCode(@override, name), diagnosticIds);

        [Theory]
        [MemberData(nameof(SadNames))]
        [MemberData(nameof(HappyNames))]
        public Task HappyExplicits(string name, params string[] diagnosticIds) => Assert(GetCode(@explicit, name), diagnosticIds);

        static string GetCode(string template, string name) => string.Format(template, name);
    }
}
