using System.Threading.Tasks;
using Particular.CodeRules.MustImplementIHandleMessages;
using Xunit;

namespace Particular.CodeRules.Tests
{
    public class IHandleMessagesTests : CSharpAnalyzerTestFixture<MustImplementIHandleMessagesAnalyzer>
    {
        [Fact]
        public Task ImplementedHandleNoToken()
        {
             string code = @"
using System.Threading.Tasks;
class C : IHandleMessages<Message>
{
    public Task Handle(Message message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }
}";
            return this.NoDiagnostic(code + referenceCode, DiagnosticIds.MustImplementIHandleMessages);
        }


        [Fact]
        public Task ImplementedHandleWithToken()
        {
            string code = @"
using System.Threading.Tasks;
class C : IHandleMessages<Message>
{
    public Task Handle(Message message, IMessageHandlerContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}";
            return this.NoDiagnostic(code + referenceCode, DiagnosticIds.MustImplementIHandleMessages);
        }


        [Fact]
        public Task ImplementedHandleAsyncNoToken()
        {
            string code = @"
using System.Threading.Tasks;
class C : IHandleMessages<Message>
{
    public Task HandleAsync(Message message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }
}";
            return this.NoDiagnostic(code + referenceCode, DiagnosticIds.MustImplementIHandleMessages);
        }


        [Fact]
        public Task ImplementedHandleAsyncWithToken()
        {
            string code = @"
using System.Threading.Tasks;
class C : IHandleMessages<Message>
{
    public Task HandleAsync(Message message, IMessageHandlerContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}";
            return this.NoDiagnostic(code + referenceCode, DiagnosticIds.MustImplementIHandleMessages);
        }






        [Fact]
        public Task NoImplementation()
        {
            const string code = @"
using System.Threading.Tasks;
class C : [|IHandleMessages<Message>|]
{
}";
            return this.HasDiagnostic(code + referenceCode, DiagnosticIds.MustImplementIHandleMessages);
        }

        [Fact]
        public Task IgnoreWhenNoInherits()
        {
            const string code = @"
using System.Threading.Tasks;
class C
{
}";
            return this.NoDiagnostic(code + referenceCode, DiagnosticIds.MustImplementIHandleMessages);
        }

        [Fact]
        public Task IgnoreWhenBaseClassUnrelated()
        {
            const string code = @"
using System.Threading.Tasks;
class C : Foo
{
}";
            return this.NoDiagnostic(code + referenceCode, DiagnosticIds.MustImplementIHandleMessages);
        }

        [Fact]
        public Task IgnoreWhenBaseClassAndIMessage()
        {
            const string code = @"
using System.Threading.Tasks;
class C : Foo, IMessage
{
}";
            return this.NoDiagnostic(code + referenceCode, DiagnosticIds.MustImplementIHandleMessages);
        }

        [Fact]
        public Task BaseClassAndIHandle()
        {
            const string code = @"
using System.Threading.Tasks;
class C : Foo, [|IHandleMessages<Message>|]
{
}";
            return this.HasDiagnostic(code + referenceCode, DiagnosticIds.MustImplementIHandleMessages);
        }

        [Fact]
        public Task DontGetTrippedUpByWeirdWhitespace()
        {
            const string code = @"
using System.Threading.Tasks;
class C : Foo, [|IHandleMessages  <  Message  >|]
{
}";
            return this.HasDiagnostic(code + referenceCode, DiagnosticIds.MustImplementIHandleMessages);
        }

        [Fact]
        public Task TwoHandlersWithMethodsOk()
        {
            const string code = @"
using System.Threading.Tasks;
class C : Foo, IHandleMessages<Message1>,
    IHandleMessages<Message2>
{
    public Task Handle(Message1 message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }

    public Task Handle(Message2 message, IMessageHandlerContext context, CancellationToken)
    {
        return Task.CompletedTask;
    }
}";
            return this.NoDiagnostic(code + referenceCode, DiagnosticIds.MustImplementIHandleMessages);
        }

        [Fact]
        public Task TwoHandlersNeedBoth()
        {
            const string code = @"
using System.Threading.Tasks;
class C : Foo, [|IHandleMessages<Message1>|],
    [|IHandleMessages<Message2>|]
{
}";
            return this.HasDiagnostic(code + referenceCode, DiagnosticIds.MustImplementIHandleMessages);
        }

        [Fact]
        public Task SagaExampleMissingIHandle()
        {
            const string code = @"
using System.Threading.Tasks;
class SomeSaga : Saga<MyData>, IAmStartedBy<Message1>,
    [|IHandleMessages<Message2>|]
{
    public Task Handle(Message1 message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }
}";
            return this.HasDiagnostic(code + referenceCode, DiagnosticIds.MustImplementIHandleMessages);
        }

        [Fact]
        public Task SagaExampleMissingIAmStarted()
        {
            const string code = @"
using System.Threading.Tasks;
class SomeSaga : Saga<MyData>, [|IAmStartedByMessages<Message1>|],
    IHandleMessages<Message2>
{
    public Task Handle(Message2 message, IMessageHandlerContext context, CancellationToken)
    {
        return Task.CompletedTask;
    }
}";
            return this.HasDiagnostic(code + referenceCode, DiagnosticIds.MustImplementIHandleMessages);
        }

        [Fact]
        public Task SagaExampleOk()
        {
            const string code = @"
using System.Threading.Tasks;
class SomeSaga : Saga<MyData>, IAmStartedByMessages<Message1>,
    IHandleMessages<Message2>
{
    public Task Handle(Message1 message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }

    public Task Handle(Message2 message, IMessageHandlerContext context, CancellationToken token)
    {
        return Task.CompletedTask;
    }
}";
            return this.NoDiagnostic(code + referenceCode, DiagnosticIds.MustImplementIHandleMessages);
        }

        [Fact]
        public Task TwoHandle()
        {
            const string code = @"
using System.Threading.Tasks;
class C : IHandleMessages<Message>
{
    public Task [|Handle|](Message message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }

    public Task [|Handle|](Message message, IMessageHandlerContext context, CancellationToken token)
    {
        return Task.CompletedTask;
    }
}";
            return this.HasDiagnostic(code + referenceCode, DiagnosticIds.TooManyIHandleMessagesImplementations);
        }

        [Fact]
        public Task TwoHandleAsync()
        {
            const string code = @"
using System.Threading.Tasks;
class C : IHandleMessages<Message>
{
    public Task [|HandleAsync|](Message message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }

    public Task [|HandleAsync|](Message message, IMessageHandlerContext context, CancellationToken token)
    {
        return Task.CompletedTask;
    }
}";
            return this.HasDiagnostic(code + referenceCode, DiagnosticIds.TooManyIHandleMessagesImplementations);
        }


        [Fact]
        public Task FourHandleMethods()
        {
            const string code = @"
using System.Threading.Tasks;
class C : IHandleMessages<Message>
{
    public Task [|Handle|](Message message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }

    public Task [|Handle|](Message message, IMessageHandlerContext context, CancellationToken token)
    {
        return Task.CompletedTask;
    }

    public Task [|HandleAsync|](Message message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }

    public Task [|HandleAsync|](Message message, IMessageHandlerContext context, CancellationToken token)
    {
        return Task.CompletedTask;
    }
}";
            return this.HasDiagnostic(code + referenceCode, DiagnosticIds.TooManyIHandleMessagesImplementations);
        }










        const string referenceCode = @"";

//public class Message { }

//namespace NServiceBus
//{

//    public interface IHandleMessages<T>
//    {
//    }

//    public interface IMessageHandlerContext {}

//}
//";
    }
}
