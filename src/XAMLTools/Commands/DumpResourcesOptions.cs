namespace XAMLTools.Commands;

using System.Threading.Tasks;
using CommandLine;
using XAMLTools.ResourceDump;

[Verb("dump-resources", HelpText = "Generate XAML color scheme files.")]
public class DumpResourcesOptions : BaseOptions
{
    [Option('a', Required = true, HelpText = "Assembly file")]
    public string AssemblyFile { get; } = null!;

    [Option('o', Required = true, HelpText = "Output path")]
    public string OutputPath { get; } = null!;

    public Task<int> Execute()
    {
        var resourceDumper = new ResourceDumper();

        resourceDumper.DumpResources(this.AssemblyFile, this.OutputPath);

        return Task.FromResult(0);
    }
}