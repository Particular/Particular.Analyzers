using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Particular.CodeRules.MustImplementIHandleMessages;
using TestHelper;
using Xunit;

namespace Particular.CodeRules.Tests
{
    public class ImplementHandleMessagesFixerTests : CodeFixVerifier
    {
        [Fact]
        public void SimpleTest()
        {
            string test = @"
public class C : IHandleMessages<Message>
{
}" + referenceCode;

            string fixTest = @"
public class C : IHandleMessages<Message>
{

    public async Task Handle(Message message, IMessageHandlerContext context)
    {
    }
}" + referenceCode;

            VerifyCSharpFix(test, fixTest, allowNewCompilerDiagnostics: true);
        }

        [Fact]
        public void KeepExistingMethods()
        {
            string test = @"
public class C : IHandleMessages<Message>
{
    private void Foo() {}

    private void Bar() {}
}" + referenceCode;

            string fixTest = @"
public class C : IHandleMessages<Message>
{
    private void Foo() {}

    private void Bar() {}

    public async Task Handle(Message message, IMessageHandlerContext context)
    {
    }
}" + referenceCode;

            VerifyCSharpFix(test, fixTest, allowNewCompilerDiagnostics: true);
        }


        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ImplementIHandleMessagesFixer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new MustImplementIHandleMessagesAnalyzer();
        }

        const string referenceCode = @"

public class Message { }

namespace NServiceBus
{

    public interface IHandleMessages<T>
    {
    }

    public interface IMessageHandlerContext { }

}";
    }
}
