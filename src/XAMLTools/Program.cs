namespace XamlTools
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime;
    using System.Threading.Tasks;
    using CommandLine;
    using XAMLTools.Helpers;
    using XAMLTools.ResourceDump;
    using XAMLTools.XAMLColorSchemeGenerator;
    using XAMLTools.XAMLCombine;

    internal class Program
    {
        private class BaseOptions
        {
            [Option('v', HelpText = "Defines if logging should be verbose")]
            public bool Verbose { get; set; }
        }

        [Verb("combine", HelpText = "Combine multiple XAML files to one target file.")]
        private class XAMLCombineOptions : BaseOptions, IXamlCombinerOptions
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

        [Verb("colorscheme", HelpText = "Generate XAML color scheme files.")]
        private class XAMLColorSchemeGeneratorOptions : BaseOptions
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

        [Verb("dump-resources", HelpText = "Generate XAML color scheme files.")]
        private class DumpResourcesOptions : BaseOptions
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

        private static async Task<int> Main(string[] args)
        {
            const string ProfileFile = "XAMLTools.profile";

            ProfileOptimization.SetProfileRoot(Path.GetTempPath());
            ProfileOptimization.StartProfile(ProfileFile);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var result = Parser.Default.ParseArguments<XAMLCombineOptions, XAMLColorSchemeGeneratorOptions, DumpResourcesOptions>(args)
                                   .MapResult(
                    async (XAMLCombineOptions options) => await options.Execute(),
                    async (XAMLColorSchemeGeneratorOptions options) => await options.Execute(),
                    async (DumpResourcesOptions options) => await options.Execute(),
                    errors => Task.FromResult(1));

                return await result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                if (Debugger.IsAttached)
                {
                    Console.ReadLine();
                }

                return 1;
            }
            finally
            {
                Console.WriteLine($"Execution time: {stopwatch.Elapsed}");
            }
        }

        private static Task<int> ErrorHandler(IEnumerable<Error> errors)
        {
            foreach (var error in errors)
            {
                Console.Error.WriteLine(error.ToString());   
            }

            return Task.FromResult(1);
        }
    }
}
