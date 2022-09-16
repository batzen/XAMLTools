namespace XamlTools;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Threading.Tasks;
using CommandLine;
using XAMLTools.Commands;

public static class Program
{
    private static async Task<int> Main(string[] args)
    {
        const string PROFILE_FILE = "XAMLTools.profile";

        ProfileOptimization.SetProfileRoot(Path.GetTempPath());
        ProfileOptimization.StartProfile(PROFILE_FILE);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = Parser.Default
                               .ParseArguments<XAMLCombineOptions, XAMLColorSchemeGeneratorOptions, DumpResourcesOptions>(args)
                               .MapResult(
                                   async (XAMLCombineOptions options) => await options.Execute(),
                                   async (XAMLColorSchemeGeneratorOptions options) => await options.Execute(),
                                   async (DumpResourcesOptions options) => await options.Execute(),
                                   ErrorHandler);

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