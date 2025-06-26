namespace Particular.Analyzers.Tests
{
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Particular.Analyzers.Tests.Helpers;

    public class DateTimeNowAnalyzerTests : AnalyzerTestFixture<DateTimeNowAnalyzer>
    {
        [Test]
        public Task SimpleTest()
        {
            const string code = @"
public class Foo
{
    public void Bar()
    {
        var t1 = [|DateTime.Now|];
        var t2 = [|DateTimeOffset.Now|];
        var t3 = this.Now;
        var t4 = new Foo().Now;

        var other = new Foo();
        var t5 = other.Now;

        Use([|DateTime.Now|]);
        Use([|DateTimeOffset.Now|]);
    }
    public int Now { get; }
    public void Use(DateTime dt) {}
    public void Use(DateTimeOffset dto) {}
}"
;
            return Assert(code, "PS0023");
        }
    }
}
