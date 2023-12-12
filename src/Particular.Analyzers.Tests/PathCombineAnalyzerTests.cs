namespace Particular.Analyzers.Tests
{
    using System.Threading.Tasks;
    using Particular.Analyzers.Tests.Helpers;
    using Xunit;
    using Xunit.Abstractions;

    public class PathCombineAnalyzerTests : AnalyzerTestFixture<PathCombineAnalyzer>
    {
        public PathCombineAnalyzerTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public Task SimpleTest()
        {
            const string code = @"
using System.IO;
public class Foo
{
    public void Bar()
    {
        Path.Combine(""blah"");
        Path.Combine(""blah"", [|""a\\b""|]);
        Path.Combine(""blah"", [|""a\\b""|], [|""a\\b""|]);
        Path.Combine(""blah"", [|""a\\b""|], [|""a\\b""|], [|""a\\b""|]);
        Path.Combine(""blah"", [|""a\\b""|], [|""a\\b""|], [|""a\\b""|], [|""a\\b""|]);

        Path.Combine(""blah"");
        Path.Combine(""blah"", [|""a/b""|]);
        Path.Combine(""blah"", [|""a/b""|], [|""a/b""|]);
        Path.Combine(""blah"", [|""a/b""|], [|""a/b""|], [|""a/b""|]);
        Path.Combine(""blah"", [|""a/b""|], [|""a/b""|], [|""a/b""|], [|""a/b""|]);
    }
}"
;
            return Assert(code, "PS0025");
        }

        [Fact]
        public Task Interpolated()
        {
            const string code = @"
using System.IO;
public class Foo
{
    public void Bar()
    {
        Path.Combine(""blah"", [|$""a\\b""|]);
    }
}"
;
            return Assert(code, "PS0025");
        }

        [Fact]
        public Task Expressions()
        {
            const string code = @"
using System.IO;
public class Foo
{
    public void Bar()
    {
        var technicallyOkStringButMustWarn = ""blah"";
        var notOkString = ""foo/bar"";
        var func = () => ""blah"";
        var cls = new Cls { Prop = ""blah"" };
        
        Path.Combine(""blah"", [|technicallyOkStringButMustWarn|], [|notOkString|], [|GetString()|], [|func()|], [|cls.Prop|]);
    }
    static string GetString() => ""blah"";
    class Cls { public string Prop { get; set; } }
}"
;
            return Assert(code, "PS0025");
        }
    }
}
