using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace EmailSink
{
    internal static class Telemetry
    {
        private static readonly string Key = TelemetryConfiguration.Active.InstrumentationKey = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", EnvironmentVariableTarget.Process);
        public static TelemetryClient Client { get; } = new TelemetryClient()
        {
            InstrumentationKey = Key
        };
    }
}
