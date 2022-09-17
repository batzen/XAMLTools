namespace XAMLTools.Commands;

using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;
using XAMLTools.ResourceDump;

[PublicAPI]
[Verb("dump-resources", HelpText = "Generate XAML color scheme files.")]
public class DumpResourcesOptions : BaseOptions
{
    [Option('a', Required = true, HelpText = "Assembly file")]
    public string AssemblyFile { get; set; } = null!;

    [Option('o', Required = true, HelpText = "Output path")]
    public string OutputPath { get; set; } = null!;

    public Task<int> Execute()
    {
        var resourceDumper = new ResourceDumper();

        resourceDumper.DumpResources(this.AssemblyFile, this.OutputPath);

        return Task.FromResult(0);
    }
}