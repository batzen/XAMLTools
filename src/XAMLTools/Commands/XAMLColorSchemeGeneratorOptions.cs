namespace XAMLTools.Commands;

using System.Threading.Tasks;
using CommandLine;
using XamlTools;
using XAMLTools.Helpers;
using XAMLTools.XAMLColorSchemeGenerator;

[Verb("colorscheme", HelpText = "Generate XAML color scheme files.")]
public class XAMLColorSchemeGeneratorOptions : BaseOptions
{
    [Option('p', Required = true, HelpText = "Parameters file")]
    public string ParametersFile { get; set; } = null!;

    [Option('t', Required = true, HelpText = "Template file")]
    public string TemplateFile { get; set; } = null!;

    [Option('o', Required = true, HelpText = "Output path")]
    public string OutputPath { get; set; } = null!;

    public Task<int> Execute()
    {
        var generator = new ColorSchemeGenerator
        {
            Logger = new ConsoleLogger
            {
                Verbose = this.Verbose
            }
        };

        MutexHelper.ExecuteLocked(() => generator.GenerateColorSchemeFiles(this.ParametersFile, this.TemplateFile, this.OutputPath), this.TemplateFile);

        return Task.FromResult(0);
    }
}