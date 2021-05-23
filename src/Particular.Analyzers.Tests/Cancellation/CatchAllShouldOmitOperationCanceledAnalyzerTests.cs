namespace Particular.Analyzers.Tests.Cancellation
{
    using System.Threading.Tasks;
    using Particular.Analyzers.Cancellation;
    using Particular.Analyzers.Tests.Helpers;
    using Xunit;
    using Xunit.Abstractions;
    using Data = System.Collections.Generic.IEnumerable<object[]>;

    public class CatchAllShouldOmitOperationCanceledAnalyzerTests : AnalyzerTestFixture<CatchAllShouldOmitOperationCanceledAnalyzer>
    {
        public CatchAllShouldOmitOperationCanceledAnalyzerTests(ITestOutputHelper output) : base(output) { }

        public static readonly Data CatchBlocks = new[]
        {
            "[|catch|] (Exception) { }",                                                        // Triggers diagnostic when passing CancellationToken in try
            "[|catch|] { }",                                                                    // Triggers diagnostic when passing CancellationToken in try
            "catch (InvalidOperationException) { } [|catch|] (Exception) { }",                  // Triggers diagnostic when passing CancellationToken in try
            "catch (InvalidOperationException) { } [|catch|] { }",                              // Triggers diagnostic when passing CancellationToken in try
            "catch (OperationCanceledException) { } catch (Exception) { }",                     // OK in all cases
            "catch (OperationCanceledException) { } catch { }",                                 // OK in all cases
            "catch (Exception ex) when (!(ex is OperationCanceledException)) { }",              // OK in all cases
            "catch (Exception ex) when (ex is not OperationCanceledException) { }",             // OK in all cases (C# 9)
            "catch (Exception oddName) when (!(oddName is OperationCanceledException)) { }",    // OK in all cases
            "catch (Exception oddName) when (oddName is not OperationCanceledException) { }",   // OK in all cases (C# 9)
            "[|catch|] (Exception ex) when (ex is OperationCanceledException) { }",             // Did it wrong, not correct
            "[|catch|] (Exception ex) when (!(ex is InvalidOperationException)) { }",           // Wrong exception type
            "[|catch|] (Exception ex) when (ex is not InvalidOperationException) { }",          // Wrong exception type (C# 9)
            "[|catch|] (Exception ex) when (!(exOther is OperationCanceledException)) { }",     // Wrong symbol
            "[|catch|] (Exception ex) when (exOther is not OperationCanceledException) { }",    // Wrong symbol (C# 9)
        }.ToData();

        [Theory]
        [MemberData(nameof(CatchBlocks))]
        public Task PassingSimpleToken(string catchBlocks)
        {
            const string PassesSimpleTokenTemplate =
@"public class Foo
{
    public async Task Bar(CancellationToken cancellationToken)
    {
        var exOther = new Exception();

        try
        {
            await Test(cancellationToken);
        }
        ##CATCH_BLOCKS##
    }
    public Task Test(CancellationToken cancellationToken) => Task.CompletedTask;
}";

            return Assert(GetCode(PassesSimpleTokenTemplate, catchBlocks), DiagnosticIds.CatchAllShouldOmitOperationCanceled);
        }

        [Theory]
        [MemberData(nameof(CatchBlocks))]
        public Task PassingTokenProperty(string catchBlocks)
        {
            const string PassesTokenPropertyTemplate =
@"public class Foo
{
    public async Task Bar(SomeContext context)
    {
        var exOther = new Exception();

        try
        {
            await Test(context.Token);
        }
        ##CATCH_BLOCKS##
    }
    public Task Test(CancellationToken cancellationToken) => Task.CompletedTask;
}
public class SomeContext
{
    public CancellationToken Token => CancellationToken.None;
}";

            return Assert(GetCode(PassesTokenPropertyTemplate, catchBlocks), DiagnosticIds.CatchAllShouldOmitOperationCanceled);
        }

        [Theory]
        [MemberData(nameof(CatchBlocks))]
        public Task MethodThatGeneratesToken(string catchBlocks)
        {
            const string PassesTokenPropertyTemplate =
@"public class Foo
{
    public async Task Bar()
    {
        var exOther = new Exception();

        try
        {
            await Test(TokenGenerator());
        }
        ##CATCH_BLOCKS##
    }
    public Task Test(CancellationToken cancellationToken) => Task.CompletedTask;
    private CancellationToken TokenGenerator() => CancellationToken.None;
}";

            return Assert(GetCode(PassesTokenPropertyTemplate, catchBlocks), DiagnosticIds.CatchAllShouldOmitOperationCanceled);
        }

        [Theory]
        [MemberData(nameof(CatchBlocks))]
        public Task FuncThatGeneratesToken(string catchBlocks)
        {
            const string PassesTokenPropertyTemplate =
@"public class Foo
{
    public async Task Bar()
    {
        var exOther = new Exception();

        try
        {
            Func<CancellationToken> func = () => CancellationToken.None;
            await Test(func());
        }
        ##CATCH_BLOCKS##
    }
    public Task Test(CancellationToken cancellationToken) => Task.CompletedTask;
    private CancellationToken TokenGenerator() => CancellationToken.None;
}";

            return Assert(GetCode(PassesTokenPropertyTemplate, catchBlocks), DiagnosticIds.CatchAllShouldOmitOperationCanceled);
        }

        [Theory]
        [MemberData(nameof(CatchBlocks))]
        public Task PassesCancellableContext(string catchBlocks)
        {
            const string PassesCancellableContextTemplate =
@"public class Foo
{
    public async Task Bar()
    {
        var exOther = new Exception();

        try
        {
            var context = new CancellableContext();
            await Test(context);
        }
        ##CATCH_BLOCKS##
    }
    Task Test(CancellableContext context = null) => Task.CompletedTask;
}
";

            return Assert(GetCode(PassesCancellableContextTemplate, catchBlocks), DiagnosticIds.CatchAllShouldOmitOperationCanceled);
        }

        [Theory]
        [MemberData(nameof(CatchBlocks))]
        public Task PassesNoTokenOrEmptyToken(string catchBlocks)
        {
            const string PassesNoTokenOrEmptyTokenTemplate =
@"class Foo
{
    async Task Bar(CancellationToken context)
    {
        var exOther = new Exception();

        try
        {
            await Test(42, true, 3.1415926);
            await Test(42, true, 3.1415926, default);
            await Test(42, true, 3.1415926, default(CancellationToken));
            await Test(42, true, 3.1415926, CancellationToken.None);

            await Test();
            await Test(default);
            await Test(default(ICancellableContext));
        }
        ##CATCH_BLOCKS##
    }
    Task Test(int i, bool b, double d, CancellationToken cancellationToken = default) => Task.CompletedTask;
    Task Test(ICancellableContext cancellableContext = default) => Task.CompletedTask;
}";

            var noDiagnosticCatchBlocks = catchBlocks.Replace("[|", "").Replace("|]", "");
            return Assert(GetCode(PassesNoTokenOrEmptyTokenTemplate, noDiagnosticCatchBlocks), DiagnosticIds.CatchAllShouldOmitOperationCanceled);
        }

        static string GetCode(string template, string catchBlocks) => template.Replace("##CATCH_BLOCKS##", catchBlocks);
    }
}
