namespace XAMLTools.Commands;

using System;
using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;
using XamlTools;
using XAMLTools.Helpers;
using XAMLTools.XAMLCombine;

[PublicAPI]
[Verb("combine", HelpText = "Combine multiple XAML files to one target file.")]
public class XAMLCombineOptions : BaseOptions, IXamlCombinerOptions
{
    [Option('s', Required = true, HelpText = "Source file containing a new line separated list of files to combine")]
    public string SourceFile { get; set; } = null!;

    [Option('t', Required = true, HelpText = "Target file")]
    public string TargetFile { get; set; } = null!;

    [Option("md", Required = false, HelpText = "Import merged dictionary references from combined files to generated")]
    public bool ImportMergedResourceDictionaryReferences { get; set; } = false;

    [Option("WriteFileHeader", Required = false, HelpText = "Write file header or not")]
    public bool WriteFileHeader { get; set; } = XAMLCombiner.WriteFileHeaderDefault;

    [Option("FileHeader", Required = false, HelpText = "Text written as the file header")]
    public string FileHeader { get; set; } = XAMLCombiner.FileHeaderDefault;

    [Option("IncludeSourceFilesInFileHeader", Required = false, HelpText = "Include source files in file header")]
    public bool IncludeSourceFilesInFileHeader { get; set; } = XAMLCombiner.IncludeSourceFilesInFileHeaderDefault;

    public Task<int> Execute()
    {
        var combiner = new XAMLCombiner
        {
            ImportMergedResourceDictionaryReferences = this.ImportMergedResourceDictionaryReferences,
            WriteFileHeader = this.WriteFileHeader,
            FileHeader = this.FileHeader,
            IncludeSourceFilesInFileHeader = this.IncludeSourceFilesInFileHeader,
            Logger = new ConsoleLogger
            {
                Verbose = this.Verbose
            }
        };

        try
        {
            MutexHelper.ExecuteLocked(() => combiner.Combine(this.SourceFile, this.TargetFile), this.TargetFile);
        }
        catch (Exception)
        {
            return Task.FromResult(1);
        }

        return Task.FromResult(0);
    }
}