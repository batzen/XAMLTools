namespace XAMLTools.Commands;

using CommandLine;
using JetBrains.Annotations;

[PublicAPI]
public class BaseOptions
{
    [Option('v', HelpText = "Defines if logging should be verbose")]
    public bool Verbose { get; set; }
}