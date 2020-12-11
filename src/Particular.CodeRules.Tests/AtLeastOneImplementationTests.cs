#if NETCOREAPP3_1

using System.Threading.Tasks;
using Particular.CodeRules.AwaitOrCaptureTasks;
using Xunit;

namespace Particular.CodeRules.Tests
{
    public class AtLeastOneImplementationTests : CSharpAnalyzerTestFixture<AtLeastOneImplementationAnalyzer>
    {
        [Fact]
        public Task NoTokenIsOk()
        {
            const string code = @"
using System.Threading.Tasks;
class C : IHandleMessages<Message>
{
    public Task Handle(Message message, IMessageHandlerContext context)
    {
        return Task.CompletedTask;
    }
}";
            return this.NoDiagnostic(code + referenceCode, DiagnosticIds.AtLeastOneImplementation);
        }

        [Fact]
        public Task TokenIsOk()
        {
            const string code = @"
using System.Threading.Tasks;
class C : IHandleMessages<Message>
{
    public Task Handle(Message message, IMessageHandlerContext context, CancellationToken token)
    {
        return Task.CompletedTask;
    }
}";
            return this.NoDiagnostic(code + referenceCode, DiagnosticIds.AtLeastOneImplementation);
        }

        [Fact]
        public Task NeitherIsBad()
        {
            const string code = @"
using System.Threading.Tasks;
class C : [|IHandleMessages<Message>|]
{
}";
            return this.HasDiagnostic(code + referenceCode, DiagnosticIds.AtLeastOneImplementation);
        }

        [Fact]
        public Task NoBaseClassIsFine()
        {
            const string code = @"
using System.Threading.Tasks;
class C
{
}";
            return this.NoDiagnostic(code + referenceCode, DiagnosticIds.AtLeastOneImplementation);
        }

        [Fact]
        public Task UnrelatedBaseClassIsFine()
        {
            const string code = @"
using System.Threading.Tasks;
class C : Foo
{
}";
            return this.NoDiagnostic(code + referenceCode, DiagnosticIds.AtLeastOneImplementation);
        }

        [Fact]
        public Task BaseAndHandlerNeedsDiagnostic()
        {
            const string code = @"
using System.Threading.Tasks;
class C : Foo, [|IHandleMessages<Message>|]
{
}";
            return this.HasDiagnostic(code + referenceCode, DiagnosticIds.AtLeastOneImplementation);
        }

        [Fact]
        public Task DontGetTrippedUpByWeirdWhitespace()
        {
            const string code = @"
using System.Threading.Tasks;
class C : Foo, [|IHandleMessages  <  Message  >|]
{
}";
            return this.HasDiagnostic(code + referenceCode, DiagnosticIds.AtLeastOneImplementation);
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
            return this.NoDiagnostic(code + referenceCode, DiagnosticIds.AtLeastOneImplementation);
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
            return this.HasDiagnostic(code + referenceCode, DiagnosticIds.AtLeastOneImplementation);
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
            return this.HasDiagnostic(code + referenceCode, DiagnosticIds.AtLeastOneImplementation);
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
            return this.HasDiagnostic(code + referenceCode, DiagnosticIds.AtLeastOneImplementation);
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

    public Task Handle(Message2 message, IMessageHandlerContext context, CancellationToken)
    {
        return Task.CompletedTask;
    }
}";
            return this.NoDiagnostic(code + referenceCode, DiagnosticIds.AtLeastOneImplementation);
        }

        [Fact]
        public Task SagaExampleMissingBoth()
        {
            const string code = @"
using System.Threading.Tasks;
class SomeSaga : Saga<MyData>, [|IAmStartedByMessages<Message1>|],
    [|IHandleMessages<Message2>|]
{
}";
            return this.HasDiagnostic(code + referenceCode, DiagnosticIds.AtLeastOneImplementation);
        }

        const string referenceCode = @"";

//public class Message { }

//namespace NServiceBus
//{

//    public interface IHandleMessages<T>
//    {
//        Task Handle(T message, IMessageHandlerContext context);
//        //{
//        //    return  Handle(message, context, CancellationToken.None);
//        //}
//    //
//        //Task Handle(T message, IMessageHandlerContext context, CancellationToken cancellationToken)
//        //{
//        //    return Handle(message, context);
//        //}
//    }

//    public interface IMessageHandlerContext {}

//}
//";
    }
}

#endif