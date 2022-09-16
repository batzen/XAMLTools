namespace XamlTools;

using System;
using XAMLTools;

public class ConsoleLogger : ILogger
{
    public bool Verbose { get; set; }

    public void Debug(string message)
    {
        if (this.Verbose == false)
        {
            return;
        }

        Console.WriteLine(message);
    }

    public void Info(string message)
    {
        if (this.Verbose == false)
        {
            return;
        }

        Console.WriteLine(message);
    }

    public void InfoImportant(string message)
    {
        Console.WriteLine(message);
    }

    public void Warn(string message)
    {
        Console.WriteLine(message);
    }

    public void Error(string message)
    {
        Console.Error.WriteLine(message);
    }
}