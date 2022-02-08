namespace XAMLTools.Tests
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using XAMLTools.XAMLCombine;

    [TestFixture]
    internal class XAMLCombinerTests
    {
        [Test]
        public async Task XamlCombinerCombine_OnMultipleInstancesOfSameNamespaceAttributeFound_DoesNotThrowTimeoutException()
        {
            int delayTime = 5000;
            var currentAssemblyDir = Path.GetDirectoryName(this.GetType().Assembly.Location)!;
            var wpfAppDirectory = Path.GetFullPath(Path.Combine(currentAssemblyDir, "../../../../src/tests/XAMLTools.WPFApp"));
            var themeFilesDirectory = Path.GetFullPath(Path.Combine(wpfAppDirectory, "Themes/Controls"));
            var themeFilePaths = Directory.GetFiles(themeFilesDirectory, "*.xaml", SearchOption.AllDirectories);

            var xamlCombiner = new XAMLCombiner();

            using (var cts = new CancellationTokenSource())
            {
                var combineTask = Task.Run(() => xamlCombiner.Combine(themeFilePaths, Path.Combine(wpfAppDirectory, "Themes/Generic.xaml")), cts.Token);
                var delayTask = Task.Delay(delayTime, cts.Token);

                var timeoutTask = Task.WhenAny(combineTask, delayTask).ContinueWith(t =>
                {
                    if (!combineTask.IsCompleted)
                    {
                        cts.Cancel();
                        throw new TimeoutException("Timeout waiting for method after " + delayTime);
                    }

                }, cts.Token);

                await timeoutTask;
            }
        }
    }
}
