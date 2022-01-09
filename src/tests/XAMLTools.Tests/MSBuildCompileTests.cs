namespace XAMLTools.Tests;

using CliWrap;
using CliWrap.Buffered;
using NUnit.Framework;

[TestFixture]
public class MSBuildCompileTests
{
    [Test]
    [TestCase("Debug")]
    [TestCase("Release")]
    public async Task CheckCompileOutputAfterGitClean(string configuration)
    {
        var currentAssemblyDir = Path.GetDirectoryName(this.GetType().Assembly.Location)!;
        var wpfAppDirectory = Path.GetFullPath(Path.Combine(currentAssemblyDir, "../../../../src/tests/XAMLTools.WPFApp"));

        Assert.That(wpfAppDirectory, Does.Exist);

#if NET472
        const string assemblyName = "XAMLTools.WPFApp.exe";
        const string framework = "net472";
#else
        const string assemblyName = "XAMLTools.WPFApp.dll";
        const string framework = "net6.0-windows";
#endif

        var binPath = Path.Combine(wpfAppDirectory, "bin", configuration, framework);

        {
            var result = await Cli.Wrap("git")
                                  .WithArguments($"clean -fxd")
                                  .WithWorkingDirectory(wpfAppDirectory)
                                  .WithValidation(CommandResultValidation.None)
                                  .ExecuteBufferedAsync();

            Assert.That(result.ExitCode, Is.EqualTo(0), result.StandardError);
        }

        {
            var result = await Cli.Wrap("dotnet")
                            .WithArguments($"build -c {configuration} /p:XAMLColorSchemeGeneratorEnabled=true /p:XAMLCombineEnabled=true /nr:false --no-dependencies -v:diag")
                            .WithWorkingDirectory(wpfAppDirectory)
                            .WithValidation(CommandResultValidation.None)
                            .ExecuteBufferedAsync();

            Assert.That(result.ExitCode, Is.EqualTo(0), result.StandardOutput);
        }

        var assemblyFile = Path.Combine(binPath, assemblyName);
        var outputPath = Path.GetDirectoryName(assemblyFile)!;

        {
            var xamlToolsExe = Path.Combine(currentAssemblyDir, "XAMLTools.exe");
            Assert.That(xamlToolsExe, Does.Exist);

            var result = await Cli.Wrap(xamlToolsExe)
                                  .WithArguments($"dump-resources -a \"{assemblyFile}\" -o \"{outputPath}\"")
                                  .WithWorkingDirectory(currentAssemblyDir)
                                  .WithValidation(CommandResultValidation.None)
                                  .ExecuteBufferedAsync();

            Assert.That(result.ExitCode, Is.EqualTo(0), result.StandardError);
        }

        {
            var resourceNames = File.ReadAllLines(Path.Combine(outputPath, "ResourceNames"));

            Assert.That(resourceNames, Is.EquivalentTo(new[]
            {
                "XAMLTools.WPFApp.g.resources",
                "XAMLTools.WPFApp.Themes.ColorScheme.Template.xaml",
                "XAMLTools.WPFApp.Themes.GeneratorParameters.json"
            }));

            var xamlResourceNames = File.ReadAllLines(Path.Combine(outputPath, "XAMLResourceNames"));

            if (configuration == "Debug")
            {
                Assert.That(xamlResourceNames, Is.EquivalentTo(
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
            else
            {
                Assert.That(xamlResourceNames, Is.EquivalentTo(
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
                                    "themes/colorschemes/light.green.highcontrast.baml"
                                }));
            }
        }
    }
}