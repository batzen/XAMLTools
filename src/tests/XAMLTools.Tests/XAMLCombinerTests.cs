namespace XAMLTools.Tests
{
    using NUnit.Framework;
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using XAMLTools.XAMLCombine;

    [TestFixture]
    internal class XAMLCombinerTests
    {
        private string targetFile = null!;

        [SetUp]
        public void SetUp()
        {
            this.targetFile = Path.Combine(Path.GetTempPath(), "XAMLCombinerTests_Generic.xaml");

            File.Delete(this.targetFile);
        }

        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        public async Task TestOutput()
        {
            var timeout = Debugger.IsAttached ? 500000 : 5000;
            var currentAssemblyDir = Path.GetDirectoryName(this.GetType().Assembly.Location)!;
            var wpfAppDirectory = Path.GetFullPath(Path.Combine(currentAssemblyDir, "../../../../src/tests/XAMLTools.WPFApp"));
            var themeFilesDirectory = Path.GetFullPath(Path.Combine(wpfAppDirectory, "Themes/Controls"));
            var themeFilePaths = Directory.GetFiles(themeFilesDirectory, "*.xaml", SearchOption.AllDirectories).Reverse().ToArray();

            var xamlCombiner = new XAMLCombiner();

            using (var cts = new CancellationTokenSource())
            {
                var combineTask = Task.Run(() => xamlCombiner.Combine(themeFilePaths, this.targetFile), cts.Token);
                var delayTask = Task.Delay(timeout, cts.Token);

                var timeoutTask = Task.WhenAny(combineTask, delayTask).ContinueWith(t =>
                {
                    if (!combineTask.IsCompleted)
                    {
                        cts.Cancel();
                        throw new TimeoutException("Timeout waiting for method after " + timeout);
                    }

                }, cts.Token);

                await timeoutTask;
            }

            await Verifier.VerifyFile(this.targetFile);
        }

        [Test]
        public async Task TestOutputWinUI()
        {
            var timeout = Debugger.IsAttached ? 500000 : 5000;
            var currentAssemblyDir = Path.GetDirectoryName(this.GetType().Assembly.Location)!;
            var wpfAppDirectory = Path.GetFullPath(Path.Combine(currentAssemblyDir, "../../../../src/tests/XAMLTools.WPFApp"));
            var themeFilesDirectory = Path.GetFullPath(Path.Combine(wpfAppDirectory, "Themes/WinUI"));
            var themeFilePaths = Directory.GetFiles(themeFilesDirectory, "*.xaml", SearchOption.AllDirectories).Reverse().ToArray();

            var xamlCombiner = new XAMLCombiner();

            using (var cts = new CancellationTokenSource())
            {
                var combineTask = Task.Run(() => xamlCombiner.Combine(themeFilePaths, this.targetFile), cts.Token);
                var delayTask = Task.Delay(timeout, cts.Token);

                var timeoutTask = Task.WhenAny(combineTask, delayTask).ContinueWith(t =>
                {
                    if (!combineTask.IsCompleted)
                    {
                        cts.Cancel();
                        throw new TimeoutException("Timeout waiting for method after " + timeout);
                    }

                }, cts.Token);

                await timeoutTask;
            }

            await Verifier.VerifyFile(this.targetFile);
        }
    }
}
