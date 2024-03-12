namespace Particular.Analyzers.Tests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Particular.Analyzers.Tests.Helpers;

    public class LoggingAnalyzerTests : AnalyzerTestFixture<LoggingAnalyzer>
    {
        [Test]
        [TestCase("DebugFormat")]
        [TestCase("InfoFormat")]
        [TestCase("WarnFormat")]
        [TestCase("ErrorFormat")]
        [TestCase("FatalFormat")]
        public Task NServiceBusLogging(string methodName)
        {
            var code = $$$""""
            public class Foo
            {
                public void Bar(NServiceBus.Logging.ILog log)
                {
                    // All matched up, good!
                    log.{{{methodName}}}("{0}", 0);
                    log.{{{methodName}}}("{0} {1}", 0, 1);
                    log.{{{methodName}}}("{0} {1} {2}", 0, 1, 2);
                    log.{{{methodName}}}("{0} {1} {2} {3}}", 0, 1, 2, 3);
                    log.{{{methodName}}}("{0} {1} {2} {3} {4}", 0, 1, 2, 3, 4);

                    // Even OK to have extra, as far as the analyzer is concerned
                    log.{{{methodName}}}("{0}", 0, 1, 2, 3, 4, 5);

                    // Not OK to have repeated
                    [|log.{{{methodName}}}("{0} {0}", 0)|];
                    // Not even if a second param is provided
                    [|log.{{{methodName}}}("{0} {0}", 0, 1)|];

                    // Doesn't matter if you rename the variable;
                    var logger = log;
                    [|logger.{{{methodName}}}("{0} {0}", 0, 1)|];

                    // Actual problematic line from Gateway (with curly brace red herring) that caused https://github.com/Particular/NServiceBus.Gateway/issues/529
                    var identity = "COMPUTERNAME";
                    [|logger.{{{methodName}}}(
            @"Did not attempt to grant user '{0}' HttpListener permissions since process is not running with elevate privileges. Processing will continue.
            To manually perform this action run the following command for each url from an admin console:
            netsh http add urlacl url={{http://URL:PORT/[PATH/] | https://URL:PORT/[PATH/]}} user=""{0}""", identity)|];

                    // Fixed version
                    logger.{{{methodName}}}(
            @"Did not attempt to grant user '{0}' HttpListener permissions since process is not running with elevate privileges. Processing will continue.
            To manually perform this action run the following command for each url from an admin console:
            netsh http add urlacl url={{http://URL:PORT/[PATH/] | https://URL:PORT/[PATH/]}} user=""{1}""", identity, identity);
                }
            }

            namespace NServiceBus.Logging
            {
                public interface ILog
                {
                    public void {{{methodName}}}(string format, params object[] args);
                }
            }
            """";

            return Assert(code, DiagnosticIds.StructuredLoggingWithRepeatedToken);
        }

        [Test]
        [TestCase("LogDebug", "Debug")]
        [TestCase("LogTrace", "Trace")]
        [TestCase("LogInformation", "Information")]
        [TestCase("LogWarning", "Warning")]
        [TestCase("LogError", "Error")]
        [TestCase("LogCritical", "Critical")]
        public Task MicrosoftExtensionsLoggingAllOk(string methodName, string logLevel)
        {
            var code = $$"""
            namespace Code
            {
                using Microsoft.Extensions.Logging;

                public class State { }

                public class Fo
                {
                    public void Bar(ILogger log)
                    {
                        var eventId = EventId.None;
                        var x = new Exception("");
                        var state = new State();

                        // Root method that doesn't use formatting
                        log.Log(LogLevel.{{logLevel}}, eventId, state, x, (a, b) => "Formatted");

                        // Basic method
                        log.{{methodName}}("plain message");
                        log.{{methodName}}("{0}", 0);
                        log.{{methodName}}("{0} {1}", 0, 1);
                        log.{{methodName}}("{0} {1} {2}", 0, 1, 2);
                        log.Log(LogLevel.{{logLevel}}, "plain message");
                        log.Log(LogLevel.{{logLevel}}, "{0}", 0);
                        log.Log(LogLevel.{{logLevel}}, "{0} {1}", 0, 1);
                        log.Log(LogLevel.{{logLevel}}, "{0} {1} {2}", 0, 1, 2);
                        log.Log(LogLevel.{{logLevel}}, "{One}", 1);
                        log.Log(LogLevel.{{logLevel}}, "{One} {Two}", 1, 2);
                        log.Log(LogLevel.{{logLevel}}, "{One} {Two} {Three}", 1, 2, 3);

                        // With EventId
                        log.{{methodName}}(eventId, "plain message");
                        log.{{methodName}}(eventId, "{0}", 0);
                        log.{{methodName}}(eventId, "{0} {1}", 0, 1);
                        log.{{methodName}}(eventId, "{0} {1} {2}", 0, 1, 2);
                        log.Log(LogLevel.{{logLevel}}, eventId, "plain message");
                        log.Log(LogLevel.{{logLevel}}, eventId, "{0}", 0);
                        log.Log(LogLevel.{{logLevel}}, eventId, "{0} {1}", 0, 1);
                        log.Log(LogLevel.{{logLevel}}, eventId, "{0} {1} {2}", 0, 1, 2);
                        log.Log(LogLevel.{{logLevel}}, eventId, "{One}", 1);
                        log.Log(LogLevel.{{logLevel}}, eventId, "{One} {Two}", 1, 2);
                        log.Log(LogLevel.{{logLevel}}, eventId, "{One} {Two} {Three}", 1, 2, 3);

                        // With Exception
                        log.{{methodName}}(x, "plain message");
                        log.{{methodName}}(x, "{0}", 0);
                        log.{{methodName}}(x, "{0} {1}", 0, 1);
                        log.{{methodName}}(x, "{0} {1} {2}", 0, 1, 2);
                        log.Log(LogLevel.{{logLevel}}, x, "plain message");
                        log.Log(LogLevel.{{logLevel}}, x, "{0}", 0);
                        log.Log(LogLevel.{{logLevel}}, x, "{0} {1}", 0, 1);
                        log.Log(LogLevel.{{logLevel}}, x, "{0} {1} {2}", 0, 1, 2);
                        log.Log(LogLevel.{{logLevel}}, x, "{One}", 1);
                        log.Log(LogLevel.{{logLevel}}, x, "{One} {Two}", 1, 2);
                        log.Log(LogLevel.{{logLevel}}, x, "{One} {Two} {Three}", 1, 2, 3);

                        // With Both
                        log.{{methodName}}(eventId, x, "plain message");
                        log.{{methodName}}(eventId, x, "{0}", 0);
                        log.{{methodName}}(eventId, x, "{0} {1}", 0, 1);
                        log.{{methodName}}(eventId, x, "{0} {1} {2}", 0, 1, 2);
                        log.Log(LogLevel.{{logLevel}}, eventId, x, "plain message");
                        log.Log(LogLevel.{{logLevel}}, eventId, x, "{0}", 0);
                        log.Log(LogLevel.{{logLevel}}, eventId, x, "{0} {1}", 0, 1);
                        log.Log(LogLevel.{{logLevel}}, eventId, x, "{0} {1} {2}", 0, 1, 2);
                        log.Log(LogLevel.{{logLevel}}, eventId, x, "{One}", 1);
                        log.Log(LogLevel.{{logLevel}}, eventId, x, "{One} {Two}", 1, 2);
                        log.Log(LogLevel.{{logLevel}}, eventId, x, "{One} {Two} {Three}", 1, 2, 3);
                    }
                }
            }
            """ + Environment.NewLine + MicrosoftLoggingCode;

            return Assert(code, DiagnosticIds.StructuredLoggingWithRepeatedToken);
        }

        [Test]
        [TestCase("LogDebug", "Debug")]
        [TestCase("LogTrace", "Trace")]
        [TestCase("LogInformation", "Information")]
        [TestCase("LogWarning", "Warning")]
        [TestCase("LogError", "Error")]
        [TestCase("LogCritical", "Critical")]
        public Task MicrosoftExtensionsLoggingNotOk(string methodName, string logLevel)
        {
            var code = $$"""
            namespace Code
            {
                using Microsoft.Extensions.Logging;

                public class State { }

                public class Fo
                {
                    public void Bar(ILogger log)
                    {
                        var eventId = EventId.None;
                        var x = new Exception("");
                        var state = new State();

                        // Basic method
                        [|log.{{methodName}}("{0} {0}", 0, 1)|];
                        [|log.Log(LogLevel.{{logLevel}}, "{0} {0}", 0, 1)|];
                        [|log.Log(LogLevel.{{logLevel}}, "{Repeated} {Repeated}", 0)|];

                        // With EventId
                        [|log.{{methodName}}(eventId, "{0} {0}", 0, 1)|];
                        [|log.Log(LogLevel.{{logLevel}}, eventId, "{0} {0}", 0, 1)|];
                        [|log.Log(LogLevel.{{logLevel}}, eventId, "{Repeated} {Repeated}", 0)|];

                        // With Exception
                        [|log.{{methodName}}(x, "{0} {0}", 0, 1)|];
                        [|log.Log(LogLevel.{{logLevel}}, x, "{0} {0}", 0, 1)|];
                        [|log.Log(LogLevel.{{logLevel}}, x, "{Repeated} {Repeated}", 0)|];

                        // With Both
                        [|log.{{methodName}}(eventId, x, "{0} {0}", 0, 1)|];
                        [|log.Log(LogLevel.{{logLevel}}, eventId, x, "{0} {0}", 0, 1)|];
                        [|log.Log(LogLevel.{{logLevel}}, eventId, x, "{Repeated} {Repeated}", 0)|];
                    }
                }
            }
            """ + Environment.NewLine + MicrosoftLoggingCode;

            return Assert(code, DiagnosticIds.StructuredLoggingWithRepeatedToken);
        }


        const string MicrosoftLoggingCode = """
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
        }
        """;
    }
}
