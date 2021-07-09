namespace Particular.Analyzers.Tests
{
    using System.Threading.Tasks;
    using Particular.Analyzers.Cancellation;
    using Particular.Analyzers.Tests.Helpers;
    using Xunit;
    using Xunit.Abstractions;

    public class DateTimeImplicitCastAnalyzerTests
        : AnalyzerTestFixture<DateTimeImplicitCastAnalyzer>
    {
        public DateTimeImplicitCastAnalyzerTests(ITestOutputHelper output) : base(output) { }

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
        DateTimeOffset a, b;
        DateTimeOffset multiple1 = DateTimeOffset.UtcNow, multiple2 = DateTimeOffset.Now;

        // Assignments of DateTimeOffset = DateTime
        [|dto = DateTime.Now|];
        [|dtoArray[0] = DateTime.Now|];
        DateTimeOffset [|dto2 = DateTime.Now|];
        DateTimeOffset [|dto3 = GetDateTime()|];
        DateTimeOffset multiple3, [|multiple4 = DateTime.Now|];
        DateTimeOffset [|multiple5 = DateTime.Now|], [|multiple6 = DateTime.Now|];
    }
    public DateTime GetDateTime()
    {
        return DateTime.Now;
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

        [Fact]
        public Task MethodReturnValue()
        {
            const string code = @"
public class Foo
{
    public DateTimeOffset Method1()
    {
        return [|DateTime.Now|];
    }
    public DateTimeOffset Method2()
    {
        var now = DateTime.Now;
        return [|now + TimeSpan.FromMinutes(10)|];
    }
    public DateTimeOffset MultipleReturns(bool utc)
    {
        if (utc)
        {
            return [|DateTime.UtcNow|];
        }
        else
        {
            return [|DateTime.Now|];
        }
    }
}";
            return Assert(code, "PS0022");
        }

        [Fact]
        public Task MethodReturnValueAsync()
        {
            const string code = @"
#pragma warning disable CS1998
public class Foo
{
    public async Task<DateTimeOffset> Method1()
    {
        return [|DateTime.Now|];
    }
    public async Task<DateTimeOffset> Method2()
    {
        var now = DateTime.Now;
        return [|now + TimeSpan.FromMinutes(10)|];
    }
    public async Task<DateTimeOffset> MultipleReturns(bool utc)
    {
        if (utc)
        {
            return [|DateTime.UtcNow|];
        }
        else
        {
            return [|DateTime.Now|];
        }
    }

    // This doesn't compile anyway
    //public Task<DateTimeOffset> NoAsyncKeyword()
    //{
    //    //return Task.FromResult(DateTime.UtcNow);
    //}
}";
            return Assert(code, "PS0022");
        }

        [Fact]
        public Task MethodParameter()
        {
            const string code = @"
public class Foo
{
    public void Bar()
    {
        // Bad: Passes a DateTime into a DateTimeOffset
        Method(1, [|DateTime.Now|], 42);
        Method(2, [|DateTime.UtcNow|], 42);
        Method(3, [|new DateTime(2000, 1, 1)|], 42);
        var now = DateTime.UtcNow;
        Method(4, [|now + TimeSpan.FromMinutes(10)|], 42);

        // OK - Correctly passes DateTimeOffset
        Method(1, DateTimeOffset.Now, 42);
        Method(2, DateTimeOffset.UtcNow, 42);
        Method(3, new DateTimeOffset(), 42);
        var now2 = DateTimeOffset.UtcNow;
        Method(4, now2 + TimeSpan.FromMinutes(10), 42);

        // OK - Passing DateTime to method that *takes* DateTime
        Method2(1, DateTime.Now, 42);
        Method2(2, DateTime.UtcNow, 42);
        Method2(3, new DateTime(), 42);
        Method2(4, now + TimeSpan.FromMinutes(10), 42);
    }
    public void Method(int a, DateTimeOffset dto, int b) {}
    public void Method2(int a, DateTime dt, int b) {}
}";
            return Assert(code, "PS0022");
        }

        [Fact]
        public Task DelegateInvocation()
        {
            const string code = @"
public class Foo
{
    Action<DateTimeOffset> fieldAction = dto => {};

    Action<DateTimeOffset> propertyAction
    {
        get { return dto => {}; }
    }

    public void Bar(Func<DateTimeOffset, bool> func)
    {
        func([|DateTime.Now|]);
        func([|DateTime.UtcNow|]);
        func(DateTimeOffset.Now);
        func(DateTimeOffset.UtcNow);

        var action = new Action<DateTimeOffset>(dto => { });
        action([|DateTime.Now|]);
        action([|DateTime.UtcNow|]);
        action(DateTimeOffset.Now);
        action(DateTimeOffset.UtcNow);

        fieldAction([|DateTime.Now|]);
        fieldAction([|DateTime.UtcNow|]);
        fieldAction(DateTimeOffset.Now);
        fieldAction(DateTimeOffset.UtcNow);

        propertyAction([|DateTime.Now|]);
        propertyAction([|DateTime.UtcNow|]);
        propertyAction(DateTimeOffset.Now);
        propertyAction(DateTimeOffset.UtcNow);
    }
}";
            return Assert(code, "PS0022");
        }

        [Fact]
        public Task DontScrewUpOnParamsArrays()
        {
            const string code = @"
public class Foo
{
    public void Bar()
    {
        Method(DateTimeOffset.UtcNow, DateTimeOffset.Now, DateTime.UtcNow, DateTime.Now);
    }
    void Method(params object[] args) {}
}";
            return Assert(code, "PS0022");
        }
    }
}
