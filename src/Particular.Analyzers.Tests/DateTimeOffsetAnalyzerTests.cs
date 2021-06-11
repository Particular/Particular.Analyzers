namespace Particular.Analyzers.Tests
{
    using System.Threading.Tasks;
    using Particular.Analyzers.Cancellation;
    using Particular.Analyzers.Tests.Helpers;
    using Xunit;
    using Xunit.Abstractions;

    public class DateTimeOffsetAnalyzerTests : AnalyzerTestFixture<DateTimeOffsetAnalyzer>
    {
        public DateTimeOffsetAnalyzerTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public Task SimpleTest()
        {
            const string code = @"
public class Foo
{
    public void Bar()
    {
        var dtoArray = new DateTimeOffset[3];

        // All OK
        DateTimeOffset dto = DateTimeOffset.Now;
        dto = DateTimeOffset.UtcNow;
        dto = new DateTimeOffset(DateTime.Now);
        dto = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Assignments of DateTimeOffset = DateTime
        [|dto = DateTime.Now|];
        [|dtoArray[0] = DateTime.Now|];
        DateTimeOffset [|dto2 = DateTime.Now|];
    }
}"
;
            return Assert(code, "PS0022");
        }

        [Fact]
        public Task Tuples()
        {
            const string code = @"
public class Foo
{
    public void Bar()
    {
        DateTimeOffset d1, d2;
        [|(d1, d2) = ReturnTuple()|];
    }
    (DateTime, DateTime) ReturnTuple() => (DateTime.Now, DateTime.UtcNow);
}"
;
            return Assert(code, "PS0022");
        }

        [Fact]
        public Task Indexer()
        {
            const string code = @"
public class Foo
{
    public void Bar()
    {
        var test = new HasIndexer();
        test[0] = DateTimeOffset.UtcNow;
        test[1] = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
        [|test[2] = DateTime.Now|];
    }
}
public class HasIndexer
{
    public DateTimeOffset this[int i]
    {
        get { return DateTimeOffset.UtcNow; }
        set { }
    }
}"
;
            return Assert(code, "PS0022");
        }
    }
}
