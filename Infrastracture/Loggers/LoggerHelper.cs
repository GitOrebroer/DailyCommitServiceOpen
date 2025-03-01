using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights;
using Core.Interfaces;

namespace Infrastracture.Loggers;

public class LoggerHelper : ILoggerHelper
{
    public TelemetryClient Telemetry { get; private set; }
    internal string MessagePrefix { get; } = "/ DailyCommitService / " + Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") + ": ";
    public LoggerHelper(TelemetryClient telemetry) => this.Telemetry = telemetry;
    public void Log(string message, bool isError)
    {
        var severityLevel = isError ? SeverityLevel.Critical : SeverityLevel.Information;
        message = this.MessagePrefix + message;

        this.Telemetry.TrackTrace(message, severityLevel);
        Console.WriteLine(message);
    }
    public void LogException(Exception message) => this.Telemetry.TrackException(message);
}
