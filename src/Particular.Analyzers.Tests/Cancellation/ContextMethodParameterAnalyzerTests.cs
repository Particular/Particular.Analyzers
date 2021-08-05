namespace Particular.Analyzers.Tests.Cancellation
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Particular.Analyzers.Cancellation;
    using Particular.Analyzers.Tests.Helpers;
    using Xunit;
    using Xunit.Abstractions;
    using Data = System.Collections.Generic.IEnumerable<object[]>;

    public class ContextMethodParameterAnalyzerTests : AnalyzerTestFixture<ContextMethodParameterAnalyzer>
    {
        // also tests transitive inheritance of CancellableContext
        static readonly string @class =
@"class MyBase : CancellableContext {{ }}

class MyClass : MyBase
{{
    void MyMethod({0}) {{ }}
}}";

        static readonly string @interface =
@"interface IMyInterface : ICancellableContext
{{
    void MyMethod({0});
}}";

        static readonly string @record =
@"record MyRecord : ICancellableContext
{{
    void MyMethod({0}) {{ }}
}}";

        static readonly string @struct =
@"struct MyStruct : ICancellableContext
{{
    void MyMethod({0}) {{ }}
}}";

        static readonly string @override =
@"class MyBase : ICancellableContext
{{
    protected virtual void MyMethod({0}) {{ }}
}}

class MyClass : MyBase, ICancellableContext
{{
    protected override void MyMethod({0}) {{ }}
}}";

        static readonly string @explicit =
@"interface IMyInterface : ICancellableContext
{{
    void MyMethod({0});
}}

class MyClass : CancellableContext, IMyInterface
{{
    void IMyInterface.MyMethod({0}) {{ }}
}}";

        static readonly string constructor =
@"class MyClass : CancellableContext
{{
    MyClass({0}) {{ }}
}}";

        static readonly string @static =
@"class MyClass : CancellableContext
{{
    static void MyMethod({0}) {{ }}
}}";

        public ContextMethodParameterAnalyzerTests(ITestOutputHelper output) : base(output) { }

        public static readonly Data SadParams = new List<string>
        {
            "CancellationToken [|foo|]",
            "object foo, CancellationToken [|bar|], CancellationToken [|baz|]",
        }.ToData();

        public static readonly Data HappyParams = new List<string>
        {
            "",
            "object foo",
        }.ToData();

        [Theory]
        [MemberData(nameof(SadParams))]
        public Task SadClasses(string @params) => Assert(GetCode(@class, @params), DiagnosticIds.CancellableContextMethodCancellationToken);

        [Theory]
        [MemberData(nameof(HappyParams))]
        public Task HappyClasses(string @params) => Assert(GetCode(@class, @params));

        [Theory]
        [MemberData(nameof(SadParams))]
        public Task SadInterfaces(string @params) => Assert(GetCode(@interface, @params), DiagnosticIds.CancellableContextMethodCancellationToken);

        [Theory]
        [MemberData(nameof(HappyParams))]
        public Task HappyInterfaces(string @params) => Assert(GetCode(@interface, @params));

        [Theory]
        [MemberData(nameof(SadParams))]
        public Task SadRecords(string @params) => Assert(GetCode(@record, @params), DiagnosticIds.CancellableContextMethodCancellationToken);

        [Theory]
        [MemberData(nameof(HappyParams))]
        public Task HappyRecords(string @params) => Assert(GetCode(@record, @params));

        [Theory]
        [MemberData(nameof(SadParams))]
        public Task SadStructs(string @params) => Assert(GetCode(@struct, @params), DiagnosticIds.CancellableContextMethodCancellationToken);

        [Theory]
        [MemberData(nameof(HappyParams))]
        public Task HappyStructs(string @params) => Assert(GetCode(@struct, @params));

        [Theory]
        [MemberData(nameof(SadParams))]
        public Task SadOverrides(string parameters) => Assert(GetCode(@override, parameters), DiagnosticIds.CancellableContextMethodCancellationToken);

        [Theory]
        [MemberData(nameof(HappyParams))]
        public Task HappyOverrides(string parameters) => Assert(GetCode(@override, parameters));

        [Theory]
        [MemberData(nameof(SadParams))]
        public Task SadExplicits(string parameters) => Assert(GetCode(@explicit, parameters), DiagnosticIds.CancellableContextMethodCancellationToken);

        [Theory]
        [MemberData(nameof(HappyParams))]
        public Task HappyExplicits(string parameters) => Assert(GetCode(@explicit, parameters));

        [Theory]
        [MemberData(nameof(SadParams))]
        [MemberData(nameof(HappyParams))]
        public Task HappyConstructors(string @params) => Assert(GetCode(constructor, @params));

        [Theory]
        [MemberData(nameof(SadParams))]
        [MemberData(nameof(HappyParams))]
        public Task HappyStatics(string @params) => Assert(GetCode(@static, @params));

        static string GetCode(string template, string @params) => string.Format(template, @params);
    }
}
