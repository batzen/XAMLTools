namespace XAMLTools;

public interface ILogger
{
    void Debug(string message);

    void Info(string message);

    void InfoImportant(string message);

    void Warn(string message);

    void Error(string message);
}