namespace XAMLTools;

using System.Diagnostics;

public class TraceLogger : ILogger
{
    public void Debug(string message)
    {
        Trace.WriteLine(message);
    }

    public void Info(string message)
    {
        Trace.WriteLine(message);
    }

    public void InfoImportant(string message)
    {
        Trace.WriteLine(message);
    }

    public void Warn(string message)
    {
        Trace.TraceWarning(message);
    }

    public void Error(string message)
    {
        Trace.TraceError(message);
    }
}