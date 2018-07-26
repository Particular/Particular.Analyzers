namespace Particular.CodeRules.Tests
{
    using System.Threading.Tasks;
    using Particular.CodeRules.AwaitOrCaptureTasks;
    using Xunit;

    public class AwaitOrCaptureTasksAnalyzerTests : CSharpAnalyzerTestFixture<AwaitOrCaptureTasksAnalyzer>
    {
        [Fact]
        public Task AwaitedTaskIsOk()
        {
            const string code = @"
using System.Threading.Tasks;
class C
{
    public async Task Foo()
    {
        await Task.Delay(1);
    }
}";
            return NoDiagnostic(code, DiagnosticIds.AwaitOrCaptureTasks);
        }



        [Fact]
        public Task CapturedTaskIsOk()
        {
            const string code = @"
using System.Threading.Tasks;
class C
{
    public Task Foo()
    {
        var task = Task.Delay(1);
        return task;
    }
}";
            return NoDiagnostic(code, DiagnosticIds.AwaitOrCaptureTasks);
        }



        [Fact]
        public Task ReturnedTaskIsOk()
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
            return NoDiagnostic(code, DiagnosticIds.AwaitOrCaptureTasks);
        }



        [Fact]
        public Task TaskIsDropped()
        {
            const string code = @"
using System.Threading.Tasks;
class C
{
    public Task Foo()
    {
        [|Task.Delay(1)|];

        return Task.CompletedTask;
    }
}";
            return HasDiagnostic(code, DiagnosticIds.AwaitOrCaptureTasks);
        }



        [Fact]
        public Task DynamicTaskIsDropped()
        {
            const string code = @"
using System.Threading.Tasks;
class C
{
    public Task Foo(dynamic x)
    {
        [|Foo(x)|];

        return Task.CompletedTask;
    }
}";
            return HasDiagnostic(code, DiagnosticIds.AwaitOrCaptureTasks);
        }

        [Fact]
        public Task GenericTaskOfValueTypeIsDropped()
        {
            const string code = @"
using System.Threading.Tasks;
class C
{
    public Task Foo()
    {
        [|Task.FromResult(0)|];

        return Task.CompletedTask;
    }
}";
            return HasDiagnostic(code, DiagnosticIds.AwaitOrCaptureTasks);
        }



        [Fact]
        public Task GenericTaskOfReferenceTypeIsDropped()
        {
            const string code = @"
using System.Threading.Tasks;
class C
{
    public Task Foo()
    {
        [|Task.FromResult(""string"")|];

        return Task.CompletedTask;
    }
}";
            return HasDiagnostic(code, DiagnosticIds.AwaitOrCaptureTasks);
        }



        [Fact]
        public Task InvokingTaskReturningFunc()
        {
            const string code = @"
using System;
using System.Threading.Tasks;
class C
{
    public Task Foo()
    {
        Func<Task> func = () => Task.Delay(0);

        [|func()|];

        return Task.CompletedTask;
    }
}";
            return HasDiagnostic(code, DiagnosticIds.AwaitOrCaptureTasks);
        }



        [Fact]
        public Task InvokingTaskReturningFunc_OkIfAwaited()
        {
            const string code = @"
using System;
using System.Threading.Tasks;
class C
{
    public Task Foo()
    {
        Func<Task> func = () => Task.Delay(0);

        await func();

        return Task.CompletedTask;
    }
}";
            return NoDiagnostic(code, DiagnosticIds.AwaitOrCaptureTasks);
        }



        [Fact]
        public Task InvokingTaskReturningDelegate()
        {
            const string code = @"
using System;
using System.Threading.Tasks;
class C
{
    public Task Foo()
    {
        TaskyDelegate del = () => Task.Delay(0);

        [|del()|];

        return Task.CompletedTask;
    }

    public delegate Task TaskyDelegate();
}";
            return HasDiagnostic(code, DiagnosticIds.AwaitOrCaptureTasks);
        }



        [Fact]
        public Task InvokingTaskReturningDelegate_OkIfAwaited()
        {
            const string code = @"
using System;
using System.Threading.Tasks;
class C
{
    public Task Foo()
    {
        TaskyDelegate del = () => Task.Delay(0);

        await del();

        return Task.CompletedTask;
    }

    public delegate Task TaskyDelegate();
}";
            return NoDiagnostic(code, DiagnosticIds.AwaitOrCaptureTasks);
        }



        [Fact]
        public Task TaskDroppedWithinFuncLambda()
        {
            const string code = @"
using System;
using System.Threading.Tasks;
class C
{
    public Task Foo()
    {
        Func<Task> func = () => { [|Task.Delay(0)|]; return Task.Completed; }

        return func();
    }
}";
            return HasDiagnostic(code, DiagnosticIds.AwaitOrCaptureTasks);
        }





        [Fact]
        public Task BigLongFuncDoesNotMatter()
        {
            const string code = @"
using System;
using System.Threading.Tasks;
class C
{
    public Task Foo()
    {
        Func<int,int,int,int,int,Task> func = (a,b,c,d,e) => { var task = Task.Delay(1); var tot = a+b+c+d+e; return task; }

        return func(1,2,3,4,5);
    }
}";
            return NoDiagnostic(code, DiagnosticIds.AwaitOrCaptureTasks);
        }
    }
}