namespace Particular.Analyzers.Tests
{
    using System;
    using System.Threading.Tasks;
    using Particular.Analyzers.Tests.Helpers;
    using Xunit;
    using Xunit.Abstractions;

    public class LoggingAnalyzerTests : AnalyzerTestFixture<LoggingAnalyzer>
    {
        public LoggingAnalyzerTests(ITestOutputHelper output) : base(output) { }

        [Theory]
        [InlineData("DebugFormat")]
        [InlineData("InfoFormat")]
        [InlineData("WarnFormat")]
        [InlineData("ErrorFormat")]
        [InlineData("FatalFormat")]
        public Task NServiceBusLogging(string methodName)
        {
            var template = @"

public class Foo
{
    public void Bar(NServiceBus.Logging.ILog log)
    {
        // All matched up, good!
        log.DebugFormat(""{0}"", 0);
        log.DebugFormat(""{0} {1}"", 0, 1);
        log.DebugFormat(""{0} {1} {2}"", 0, 1, 2);
        log.DebugFormat(""{0} {1} {2} {3}}"", 0, 1, 2, 3);
        log.DebugFormat(""{0} {1} {2} {3} {4}"", 0, 1, 2, 3, 4);

        // Even OK to have extra, as far as the analyzer is concerned
        log.DebugFormat(""{0}"", 0, 1, 2, 3, 4, 5);

        // Not OK to have repeated
        [|log.DebugFormat(""{0} {0}"", 0)|];
        // Not even if a second param is provided
        [|log.DebugFormat(""{0} {0}"", 0, 1)|];

        // Doesn't matter if you rename the variable;
        var logger = log;
        [|logger.DebugFormat(""{0} {0}"", 0, 1)|];

        // Actual problematic line from Gateway (with curly brace red herring) that caused https://github.com/Particular/NServiceBus.Gateway/issues/529
        var identity = ""COMPUTERNAME"";
        [|logger.DebugFormat(
@""Did not attempt to grant user '{0}' HttpListener permissions since process is not running with elevate privileges. Processing will continue.
To manually perform this action run the following command for each url from an admin console:
netsh http add urlacl url={{http://URL:PORT/[PATH/] | https://URL:PORT/[PATH/]}} user=""""{0}"""""", identity)|];

        // Fixed version
        logger.DebugFormat(
@""Did not attempt to grant user '{0}' HttpListener permissions since process is not running with elevate privileges. Processing will continue.
To manually perform this action run the following command for each url from an admin console:
netsh http add urlacl url={{http://URL:PORT/[PATH/] | https://URL:PORT/[PATH/]}} user=""""{1}"""""", identity, identity);
    }
}

namespace NServiceBus.Logging
{
    public interface ILog
    {
        public void DebugFormat(string format, params object[] args);
    }
}";

            var code = template.Replace("DebugFormat", methodName);
            return Assert(code, DiagnosticIds.StructuredLoggingWithRepeatedToken);
        }

        [Theory]
        [InlineData("LogDebug", "Debug")]
        [InlineData("LogTrace", "Trace")]
        [InlineData("LogInformation", "Information")]
        [InlineData("LogWarning", "Warning")]
        [InlineData("LogError", "Error")]
        [InlineData("LogCritical", "Critical")]
        public Task MicrosoftExtensionsLoggingAllOk(string methodName, string logLevel)
        {
            var template = @"
namespace Code
{
    using Microsoft.Extensions.Logging;

    public class State { }

    public class Fo
    {
        public void Bar(ILogger log)
        {
            var eventId = EventId.None;
            var x = new Exception("""");
            var state = new State();

            // Root method that doesn't use formatting
            log.Log(LogLevel.LOGLEVEL, eventId, state, x, (a, b) => ""Formatted"");

            // Basic method
            log.METHODNAME(""plain message"");
            log.METHODNAME(""{0}"", 0);
            log.METHODNAME(""{0} {1}"", 0, 1);
            log.METHODNAME(""{0} {1} {2}"", 0, 1, 2);
            log.Log(LogLevel.LOGLEVEL, ""plain message"");
            log.Log(LogLevel.LOGLEVEL, ""{0}"", 0);
            log.Log(LogLevel.LOGLEVEL, ""{0} {1}"", 0, 1);
            log.Log(LogLevel.LOGLEVEL, ""{0} {1} {2}"", 0, 1, 2);
            log.Log(LogLevel.LOGLEVEL, ""{One}"", 1);
            log.Log(LogLevel.LOGLEVEL, ""{One} {Two}"", 1, 2);
            log.Log(LogLevel.LOGLEVEL, ""{One} {Two} {Three}"", 1, 2, 3);

            // With EventId
            log.METHODNAME(eventId, ""plain message"");
            log.METHODNAME(eventId, ""{0}"", 0);
            log.METHODNAME(eventId, ""{0} {1}"", 0, 1);
            log.METHODNAME(eventId, ""{0} {1} {2}"", 0, 1, 2);
            log.Log(LogLevel.LOGLEVEL, eventId, ""plain message"");
            log.Log(LogLevel.LOGLEVEL, eventId, ""{0}"", 0);
            log.Log(LogLevel.LOGLEVEL, eventId, ""{0} {1}"", 0, 1);
            log.Log(LogLevel.LOGLEVEL, eventId, ""{0} {1} {2}"", 0, 1, 2);
            log.Log(LogLevel.LOGLEVEL, eventId, ""{One}"", 1);
            log.Log(LogLevel.LOGLEVEL, eventId, ""{One} {Two}"", 1, 2);
            log.Log(LogLevel.LOGLEVEL, eventId, ""{One} {Two} {Three}"", 1, 2, 3);

            // With Exception
            log.METHODNAME(x, ""plain message"");
            log.METHODNAME(x, ""{0}"", 0);
            log.METHODNAME(x, ""{0} {1}"", 0, 1);
            log.METHODNAME(x, ""{0} {1} {2}"", 0, 1, 2);
            log.Log(LogLevel.LOGLEVEL, x, ""plain message"");
            log.Log(LogLevel.LOGLEVEL, x, ""{0}"", 0);
            log.Log(LogLevel.LOGLEVEL, x, ""{0} {1}"", 0, 1);
            log.Log(LogLevel.LOGLEVEL, x, ""{0} {1} {2}"", 0, 1, 2);
            log.Log(LogLevel.LOGLEVEL, x, ""{One}"", 1);
            log.Log(LogLevel.LOGLEVEL, x, ""{One} {Two}"", 1, 2);
            log.Log(LogLevel.LOGLEVEL, x, ""{One} {Two} {Three}"", 1, 2, 3);

            // With Both
            log.METHODNAME(eventId, x, ""plain message"");
            log.METHODNAME(eventId, x, ""{0}"", 0);
            log.METHODNAME(eventId, x, ""{0} {1}"", 0, 1);
            log.METHODNAME(eventId, x, ""{0} {1} {2}"", 0, 1, 2);
            log.Log(LogLevel.LOGLEVEL, eventId, x, ""plain message"");
            log.Log(LogLevel.LOGLEVEL, eventId, x, ""{0}"", 0);
            log.Log(LogLevel.LOGLEVEL, eventId, x, ""{0} {1}"", 0, 1);
            log.Log(LogLevel.LOGLEVEL, eventId, x, ""{0} {1} {2}"", 0, 1, 2);
            log.Log(LogLevel.LOGLEVEL, eventId, x, ""{One}"", 1);
            log.Log(LogLevel.LOGLEVEL, eventId, x, ""{One} {Two}"", 1, 2);
            log.Log(LogLevel.LOGLEVEL, eventId, x, ""{One} {Two} {Three}"", 1, 2, 3);
        }
    }
}
";

            var code = template.Replace("METHODNAME", methodName).Replace("LOGLEVEL", logLevel) + Environment.NewLine + MicrosoftLoggingCode;
            return Assert(code, DiagnosticIds.StructuredLoggingWithRepeatedToken);
        }

        [Theory]
        [InlineData("LogDebug", "Debug")]
        [InlineData("LogTrace", "Trace")]
        [InlineData("LogInformation", "Information")]
        [InlineData("LogWarning", "Warning")]
        [InlineData("LogError", "Error")]
        [InlineData("LogCritical", "Critical")]
        public Task MicrosoftExtensionsLoggingNotOk(string methodName, string logLevel)
        {
            var template = @"
namespace Code
{
    using Microsoft.Extensions.Logging;

    public class State { }

    public class Fo
    {
        public void Bar(ILogger log)
        {
            var eventId = EventId.None;
            var x = new Exception("""");
            var state = new State();

            // Basic method
            [|log.METHODNAME(""{0} {0}"", 0, 1)|];
            [|log.Log(LogLevel.LOGLEVEL, ""{0} {0}"", 0, 1)|];
            [|log.Log(LogLevel.LOGLEVEL, ""{Repeated} {Repeated}"", 0)|];

            // With EventId
            [|log.METHODNAME(eventId, ""{0} {0}"", 0, 1)|];
            [|log.Log(LogLevel.LOGLEVEL, eventId, ""{0} {0}"", 0, 1)|];
            [|log.Log(LogLevel.LOGLEVEL, eventId, ""{Repeated} {Repeated}"", 0)|];

            // With Exception
            [|log.METHODNAME(x, ""{0} {0}"", 0, 1)|];
            [|log.Log(LogLevel.LOGLEVEL, x, ""{0} {0}"", 0, 1)|];
            [|log.Log(LogLevel.LOGLEVEL, x, ""{Repeated} {Repeated}"", 0)|];

            // With Both
            [|log.METHODNAME(eventId, x, ""{0} {0}"", 0, 1)|];
            [|log.Log(LogLevel.LOGLEVEL, eventId, x, ""{0} {0}"", 0, 1)|];
            [|log.Log(LogLevel.LOGLEVEL, eventId, x, ""{Repeated} {Repeated}"", 0)|];
        }
    }
}
";

            var code = template.Replace("METHODNAME", methodName).Replace("LOGLEVEL", logLevel) + Environment.NewLine + MicrosoftLoggingCode;
            return Assert(code, DiagnosticIds.StructuredLoggingWithRepeatedToken);
        }


        const string MicrosoftLoggingCode = @"
#nullable enable
namespace Microsoft.Extensions.Logging
{
    public enum LogLevel { Trace, Debug, Information, Warning, Error, Critical, None }
    public readonly struct EventId
    {
        public static readonly EventId None = new EventId();
    }
    public interface ILogger
    {
	    void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter);
    }
    public static class LoggerExtensions
    {
        public static void LogDebug(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args) { }
        public static void LogDebug(this ILogger logger, EventId eventId, string? message, params object?[] args) { }
        public static void LogDebug(this ILogger logger, Exception? exception, string? message, params object?[] args) { }
        public static void LogDebug(this ILogger logger, string? message, params object?[] args) { }
        public static void LogTrace(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args) { }
        public static void LogTrace(this ILogger logger, EventId eventId, string? message, params object?[] args) { }
        public static void LogTrace(this ILogger logger, Exception? exception, string? message, params object?[] args) { }
        public static void LogTrace(this ILogger logger, string? message, params object?[] args) { }
        public static void LogInformation(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args) { }
        public static void LogInformation(this ILogger logger, EventId eventId, string? message, params object?[] args) { }
        public static void LogInformation(this ILogger logger, Exception? exception, string? message, params object?[] args) { }
        public static void LogInformation(this ILogger logger, string? message, params object?[] args) { }
        public static void LogWarning(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args) { }
        public static void LogWarning(this ILogger logger, EventId eventId, string? message, params object?[] args) { }
        public static void LogWarning(this ILogger logger, Exception? exception, string? message, params object?[] args) { }
        public static void LogWarning(this ILogger logger, string? message, params object?[] args) { }
        public static void LogError(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args) { }
        public static void LogError(this ILogger logger, EventId eventId, string? message, params object?[] args) { }
        public static void LogError(this ILogger logger, Exception? exception, string? message, params object?[] args) { }
        public static void LogError(this ILogger logger, string? message, params object?[] args) { }
        public static void LogCritical(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args) { }
        public static void LogCritical(this ILogger logger, EventId eventId, string? message, params object?[] args) { }
        public static void LogCritical(this ILogger logger, Exception? exception, string? message, params object?[] args) { }
        public static void LogCritical(this ILogger logger, string? message, params object?[] args) { }
        public static void Log(this ILogger logger, LogLevel logLevel, string? message, params object?[] args) { }
        public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, string? message, params object?[] args) { }
        public static void Log(this ILogger logger, LogLevel logLevel, Exception? exception, string? message, params object?[] args) { }
        public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, Exception? exception, string? message, params object?[] args) { }
    }
}";
    }
}
