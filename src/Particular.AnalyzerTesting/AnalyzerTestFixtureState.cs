namespace Particular.AnalyzerTesting;

using System;

static class AnalyzerTestFixtureState
{
    internal static readonly bool VerboseLogging = Environment.GetEnvironmentVariable("CI") != "true" || Environment.GetEnvironmentVariable("VERBOSE_TEST_LOGGING")?.ToLower() == "true";
}