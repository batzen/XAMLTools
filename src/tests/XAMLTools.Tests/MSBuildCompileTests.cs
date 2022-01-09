namespace XAMLTools.Tests;

using System.Collections;
using System.Diagnostics;
using System.Reflection;
using CliWrap;
using CliWrap.Buffered;
using NUnit.Framework;

[TestFixture]
public class MSBuildCompileTests
{
    [Test]
    [TestCase("Debug")]
    //[TestCase("Release")]
    public async Task CheckCompileOutputAfterGitClean(string configuration)
    {
        var currentDir = Environment.CurrentDirectory;
        var wpfAppDirectory = Path.GetFullPath("../../../../src/tests/XAMLTools.WPFApp");

#if NET472
        const string framework = "net472";
#else
        const string framework = "net6.0-windows";
#endif

        var binPath = Path.Combine(wpfAppDirectory, "bin", configuration, framework);

        Assert.That(wpfAppDirectory, Does.Exist);

        {
            using var proc = Process.Start(new ProcessStartInfo("git", "clean -fxd") { WorkingDirectory = wpfAppDirectory });
            Assert.That(proc, Is.Not.Null);

            proc!.WaitForExit();

            Assert.That(proc.ExitCode, Is.EqualTo(0));
        }

        {
            var result = await Cli.Wrap("dotnet")
                            .WithArguments($"build -c {configuration} /p:XAMLColorSchemeGeneratorEnabled=true /p:XAMLCombineEnabled=true /nr:false --no-dependencies") // -v:diag")
                            .WithWorkingDirectory(wpfAppDirectory)
                            .WithValidation(CommandResultValidation.None)
                            .ExecuteBufferedAsync();

            Assert.That(result.ExitCode, Is.EqualTo(0), result.StandardOutput);
        }

        {
            var assemblyFile = Path.Combine(binPath, "XAMLTools.WPFApp.dll");
            var assembly = Assembly.LoadFile(assemblyFile);

            var resourceNames = assembly.GetManifestResourceNames();

            Assert.That(resourceNames, Is.EquivalentTo(new[]
            {
                "XAMLTools.WPFApp.g.resources",
                "XAMLTools.WPFApp.Themes.ColorScheme.Template.xaml",
                "XAMLTools.WPFApp.Themes.GeneratorParameters.json"
            }));

            using var xamlResourcesStream = assembly.GetManifestResourceStream("XAMLTools.WPFApp.g.resources")!;
            using var reader = new System.Resources.ResourceReader(xamlResourcesStream);
            var xamlResourceEntries = reader.Cast<DictionaryEntry>().Select(entry => (string)entry.Key).ToArray();
            Assert.That(xamlResourceEntries, Is.EquivalentTo(
                            new[]
                            {
                                "themes/colorschemes/light.yellow.colorful.baml",
                                "themes/colorschemes/dark.yellow.colorful.baml",
                                "themes/colorschemes/light.yellow.baml",
                                "themes/colorschemes/dark.blue.colorful.baml",
                                "themes/colorschemes/dark.green.colorful.highcontrast.baml",
                                "themes/colorschemes/dark.yellow.baml",
                                "themes/generic.baml",
                                "themes/colorschemes/light.blue.baml",
                                "themes/colorschemes/light.blue.colorful.baml",
                                "mainwindow.baml",
                                "themes/colorschemes/dark.green.highcontrast.baml",
                                "themes/colorschemes/dark.blue.baml",
                                "themes/colorschemes/light.green.highcontrast.baml",
                                "themes/controls/control2.baml",
                                "themes/controls/control1.baml",
                            }));
        }
    }
}