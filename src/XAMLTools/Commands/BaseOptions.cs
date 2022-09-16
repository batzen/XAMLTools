namespace XAMLTools.Commands;

using CommandLine;

public class BaseOptions
{
    [Option('v', HelpText = "Defines if logging should be verbose")]
    public bool Verbose { get; set; }
}