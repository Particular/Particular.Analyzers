using System.Threading.Tasks;
using Particular.CodeRules.ConfigureAwait;
using Xunit;

namespace Particular.CodeRules.Tests
{
    public class ConfigureAwaitAnalyzerTests : CSharpAnalyzerTestFixture<ConfigureAwaitAnalyzer>
    {
        [Fact]
        public Task AwaitMissingConfigureAwait()
        {
            const string code = @"
using System.Threading.Tasks;
class C
{
    public async Task Foo()
    {
        [|await Task.Delay(1)|];
    }
}";
            return HasDiagnostic(code, DiagnosticIds.UseConfigureAwait);
        }

        [Fact]
        public Task TaskDoesNotNeedConfigureAwait()
        {
            const string code = @"
using System.Threading.Tasks;
class C
{
    public Task Foo()
    {
        return Task.Delay(1);
    }
}";
            return NoDiagnostic(code, DiagnosticIds.UseConfigureAwait);
        }
    }
}