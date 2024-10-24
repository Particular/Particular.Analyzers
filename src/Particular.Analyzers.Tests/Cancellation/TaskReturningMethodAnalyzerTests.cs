namespace Particular.Analyzers.Tests.Cancellation
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;
    using Particular.Analyzers.Cancellation;
    using Particular.Analyzers.Tests.Helpers;
    using Data = System.Collections.Generic.IEnumerable<object[]>;

    public class TaskReturningMethodAnalyzerTests : AnalyzerTestFixture<TaskReturningMethodAnalyzer>
    {
        static readonly string method =
@"class MyClass<T> where T : CancellableContext
{{
    {0} [|MyMethod|]({1}) => throw new Exception();
}}";

        static readonly string @delegate =
@"class MyClass<T> where T : CancellableContext
{{
    delegate {0} [|MyMethod|]({1});
}}";

        static readonly string @override =
@"class MyBase<T> where T : CancellableContext
{{
#pragma warning disable
    protected virtual {0} MyMethod({1}) => throw new Exception();
#pragma warning restore
}}

class MyClass<T> : MyBase<T> where T : CancellableContext
{{
    protected override {0} MyMethod({1}) => throw new Exception();
}}";

        static readonly string @explicit =
@"interface IMyInterface<T> where T : CancellableContext
{{
#pragma warning disable
    {0} MyMethod({1});
#pragma warning restore
}}

class MyClass<T> : IMyInterface<T> where T : CancellableContext
{{
    {0} IMyInterface<T>.MyMethod({1}) => throw new Exception();
}}";

        static readonly string context =
@"class MyClass<T> : CancellableContext where T : CancellableContext
{{    
    {0} MyMethod({1}) => throw new Exception();
}}";

        static readonly string test =
@"
namespace NUnit.Framework
{{
    class OneTimeSetUpAttribute : System.Attribute {{ }}
    class OneTimeTearDownAttribute : System.Attribute {{ }}
    class SetUpAttribute : System.Attribute {{ }}
    class TearDownAttribute : System.Attribute {{ }}
    class TestAttribute : System.Attribute {{ }}
    class TestCaseAttribute : System.Attribute {{ }}
    class TestCaseSourceAttribute : System.Attribute {{ }}
    class TheoryAttribute : System.Attribute {{ }}
}}

namespace Xunit
{{
    class FactAttribute : System.Attribute {{ }}
    class TheoryAttribute : FactAttribute {{ }}
}}

class MyClass<T> where T : CancellableContext
{{
    {0}
    {1} [|MyMethod|]({2}) => throw new Exception();
}}";

        static readonly string entryPoint =
@"namespace MyNamespace
{
    class Program
    {
        static Task Main() => throw new Exception();
    }
}";

        static readonly string asyncDisposable =
@"namespace MyNamespace
{
    class MyAsyncDisposableClass : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => throw new Exception();
    }
}";

        static readonly string notAsyncDisposable =
@"namespace MyNamespace
{{
    class MyFakeAsyncDisposableClass
    {{
        public {0} [|DisposeAsync|]({1}) => throw new Exception();
    }}
}}";

        static readonly List<string> notTaskTypes = ["void", "object", "IMessage"];

        static readonly List<string> taskTypes =
        [
            "Task",
            "Task<string>",
#if NET
            "ValueTask",
            "ValueTask<string>",
#endif
        ];

        static readonly List<string> notTaskParams =
        [
            "",
            "object foo",
        ];

        static readonly List<string> taskParams =
        [
            "CancellationToken cancellationToken",
            "object foo, CancellationToken cancellationToken",
            "ICancellableContext foo",
            "ICancellableContext foo, object bar",
            "CancellableContext foo",
            "CancellableContext foo, object bar",
            "T foo",
            "T foo, object bar",
        ];

        public static readonly Data TestAttributes = new List<string>
        {
            "[NUnit.Framework.OneTimeSetUp]",
            "[NUnit.Framework.OneTimeTearDown]",
            "[NUnit.Framework.SetUp]",
            "[NUnit.Framework.TearDown]",
            "[NUnit.Framework.Test]",
            "[NUnit.Framework.TestCase]",
            "[NUnit.Framework.TestCaseSource]",
            "[NUnit.Framework.Theory]",
            "[Xunit.Fact]",
            "[Xunit.Theory]"
        }.ToData();

        public static Data SadData => taskTypes.SelectMany(type => notTaskParams.Select(@params => (type, @params))).ToData();

        public static Data HappyData =>
            taskTypes.SelectMany(type => taskParams.Select(@params => (type, @params)))
            .Concat(notTaskTypes.SelectMany(type => notTaskParams.Concat(taskParams).Select(@params => (type, @params))))
            .ToData();

        [Test]
        [TestCaseSource(nameof(SadData))]
        public Task SadMethods(string returnType, string @params) => Assert(GetCode(method, returnType, @params), DiagnosticIds.TaskReturningMethodNoCancellation);

        [Test]
        [TestCaseSource(nameof(HappyData))]
        public Task HappyMethods(string returnType, string @params) => Assert(GetCode(method, returnType, @params));

        [Test]
        [TestCaseSource(nameof(SadData))]
        public Task SadDelegates(string returnType, string @params) => Assert(GetCode(@delegate, returnType, @params), DiagnosticIds.TaskReturningMethodNoCancellation);

        [Test]
        [TestCaseSource(nameof(HappyData))]
        public Task HappyDelegates(string returnType, string @params) => Assert(GetCode(@delegate, returnType, @params));

        [Test]
        [TestCaseSource(nameof(SadData))]
        [TestCaseSource(nameof(HappyData))]
        public Task HappyOverrides(string returnType, string @params) => Assert(GetCode(@override, returnType, @params));

        [Test]
        [TestCaseSource(nameof(SadData))]
        [TestCaseSource(nameof(HappyData))]
        public Task HappyExplicits(string returnType, string @params) => Assert(GetCode(@explicit, returnType, @params));

        [Test]
        [TestCaseSource(nameof(SadData))]
        [TestCaseSource(nameof(HappyData))]
        public Task HappyContexts(string returnType, string @params) => Assert(GetCode(context, returnType, @params));

        [Test]
        [TestCaseSource(nameof(TestAttributes))]
        public Task HappyTests(string attribute) => Assert(GetTestCode(attribute, taskTypes.First(), notTaskParams.First()));

        [Test]
        public Task HappyEntryPoint() => Assert(entryPoint, c => c.CompilationOptions = new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        [Test]
        public Task HappyAsyncDisposable() => Assert(asyncDisposable);

        [Test]
        [TestCaseSource(nameof(SadData))]
        public Task SadAsyncDisposable(string returnType, string @params) => Assert(GetCode(notAsyncDisposable, returnType, @params), DiagnosticIds.TaskReturningMethodNoCancellation);

        static string GetCode(string template, string returnType, string @params) => string.Format(template, returnType, @params);

        static string GetTestCode(string attribute, string returnType, string @params) => string.Format(test, attribute, returnType, @params);
    }
}
