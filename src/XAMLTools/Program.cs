namespace XamlTools
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime;
    using System.Threading;
    using System.Threading.Tasks;
    using CommandLine;
    using XAMLTools.XAMLColorSchemeGenerator;
    using XAMLTools.XAMLCombine;

    internal class Program
    {

        [Verb("combine", HelpText = "Combine multiple XAML files to one target file.")]
        private class XAMLCombineOptions
        {
            [Option('s', Required = true, HelpText = "Source file containing a new line separated list of files to combine")]
            public string SourceFile { get; set; } = null!;

            [Option('t', Required = true, HelpText = "Target file")]
            public string TargetFile { get; set; } = null!;

            public Task<int> Execute()
            {
                var combiner = new XAMLCombiner();
                combiner.Combine(this.SourceFile, this.TargetFile);

                return Task.FromResult(0);
            }
        }

        [Verb("colorscheme", HelpText = "Generate XAML color scheme files.")]
        private class XAMLColorSchemeGeneratorOptions
        {
            [Option('p', Required = true, HelpText = "Parameters file")]
            public string ParametersFile { get; set; } = null!;

            [Option('t', Required = true, HelpText = "Template file")]
            public string TemplateFile { get; set; } = null!;

            [Option('o', Required = true, HelpText = "Output path")]
            public string OutputPath { get; set; } = null!;

            public Task<int> Execute()
            {
                using (var mutex = Lock(this.ParametersFile))
                {
                    try
                    {
                        var generator = new ColorSchemeGenerator();

                        generator.GenerateColorSchemeFiles(this.ParametersFile, this.TemplateFile, this.OutputPath);
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }

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
                var result = Parser.Default.ParseArguments<XAMLCombineOptions, XAMLColorSchemeGeneratorOptions>(args)
                                   .MapResult(
                    async (XAMLCombineOptions options) => await options.Execute(),
                    async (XAMLColorSchemeGeneratorOptions options) => await options.Execute(),
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

        private static Mutex Lock(string file)
        {           
            var mutexName = "Local\\XamlTools_" + Path.GetFileName(file);

            var mutex = new Mutex(false, mutexName);

            if (mutex.WaitOne(TimeSpan.FromSeconds(10)) == false)
            {
                throw new TimeoutException("Another instance of this application blocked the concurrent execution.");
            }
            
            return mutex;
        }
    }
}