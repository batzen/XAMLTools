namespace XAMLTools.Tests.TestHelpers;

public class TestLogger : ILogger
{
    public List<string> Warnings { get; } = new();

    public List<string> Errors { get; } = new();

    public void Debug(string message)
    {
    }

    public void Info(string message)
    {
    }

    public void InfoImportant(string message)
    {
    }

    public void Warn(string message)
    {
        this.Warnings.Add(message);
    }

    public void Error(string message)
    {
        this.Errors.Add(message);
    }
}