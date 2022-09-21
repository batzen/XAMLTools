namespace XAMLTools.Tests
{
    using NUnit.Framework;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using XAMLTools.Tests.TestHelpers;
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

                    Assert.That(combineTask.Exception, Is.Null, combineTask.Exception?.ToString());
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

                    Assert.That(combineTask.Exception, Is.Null, combineTask.Exception?.ToString());
                }, cts.Token);

                await timeoutTask;
            }

            await Verifier.VerifyFile(this.targetFile);
        }
        
        [Test]
        public void TestDuplicateNamespaces()
        {
            var currentAssemblyDir = Path.GetDirectoryName(this.GetType().Assembly.Location)!;
            var wpfAppDirectory = Path.GetFullPath(Path.Combine(currentAssemblyDir, "../../../../src/tests/XAMLTools.WPFApp"));
            var themeFilesDirectory = Path.GetFullPath(Path.Combine(wpfAppDirectory, "Themes/DuplicateNamespaces"));
            var themeFilePaths = Directory.GetFiles(themeFilesDirectory, "*.xaml", SearchOption.AllDirectories).Reverse().ToArray();

            var xamlCombiner = new XAMLCombiner();

            Assert.That(() => xamlCombiner.Combine(themeFilePaths, this.targetFile),
                        Throws.Exception
                              .With.Message
                              .Contains("Namespace name \"controls\" with different values was seen in "));
        }
        
        [Test]
        public void TestDuplicateKeys()
        {
            var currentAssemblyDir = Path.GetDirectoryName(this.GetType().Assembly.Location)!;
            var wpfAppDirectory = Path.GetFullPath(Path.Combine(currentAssemblyDir, "../../../../src/tests/XAMLTools.WPFApp"));
            var themeFilesDirectory = Path.GetFullPath(Path.Combine(wpfAppDirectory, "Themes/DuplicateKeys"));
            var themeFilePaths = Directory.GetFiles(themeFilesDirectory, "*.xaml", SearchOption.AllDirectories).Reverse().ToArray();

            var testLogger = new TestLogger();

            var xamlCombiner = new XAMLCombiner
            {
                Logger = testLogger
            };
            xamlCombiner.Combine(themeFilePaths, this.targetFile);

            Assert.That(testLogger.Errors, Is.Empty);
            Assert.That(testLogger.Warnings, Has.Count.EqualTo(1));
            Assert.That(testLogger.Warnings[0], Does.StartWith("Key \"DuplicateDifferentContent\" was found in multiple imported files and was skipped.\r\nAt: 9:6"));
        }
    }
}
