namespace XAMLTools.MSBuild;

using System.Diagnostics;
using Microsoft.Build.Framework;
using ILogger = XAMLTools.ILogger;

public class Logger : ILogger
{
    private readonly IBuildEngine buildEngine;
    private readonly string senderName;

    public Logger(IBuildEngine buildEngine, string senderName)
    {
        this.buildEngine = buildEngine;
        this.senderName = senderName;
    }

    public void Debug(string message)
    {
        this.buildEngine.LogMessageEvent(new(message, string.Empty, this.senderName, MessageImportance.Low));
    }

    public void Info(string message)
    {
        this.buildEngine.LogMessageEvent(new(message, string.Empty, this.senderName, MessageImportance.Normal));
    }

    public void InfoImportant(string message)
    {
        this.buildEngine.LogMessageEvent(new(message, string.Empty, this.senderName, MessageImportance.High));
    }

    public void Warn(string message)
    {
        this.buildEngine.LogWarningEvent(new(string.Empty, string.Empty, string.Empty, 0, 0, 0, 0, message, string.Empty, this.senderName));
    }

    public void Error(string message)
    {
        this.buildEngine.LogErrorEvent(new(string.Empty, string.Empty, string.Empty, 0, 0, 0, 0, message, string.Empty, this.senderName));
    }
}