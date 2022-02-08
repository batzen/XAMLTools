namespace XAMLTools.Tests
{
    using NUnit.Framework;
    using System;
    using System.Threading.Tasks;
    using XAMLTools.Helpers;

    [TestFixture]
    public class MutexHelperTests
    {
        [Test]
        public void MutexHelperTestTimeout()
        {
            var t1 = Task.Factory.StartNew(() => ThreadStartFunction());
            var t2 = Task.Factory.StartNew(() => ThreadStartFunction());
            var t3 = Task.Factory.StartNew(() => ThreadStartFunction());
            var t4 = Task.Factory.StartNew(() => ThreadStartFunction());
            var t5 = Task.Factory.StartNew(() => ThreadStartFunction());

            try
            {
                Task.WaitAll(t1, t2, t3, t4, t5);
            }
            catch (AggregateException ex)
            {
                // Expect TimeoutException, Mutex shoudn't throw any ApplicationException
                var innerException = ex.InnerException;
                Assert.IsTrue(innerException is TimeoutException, $"InnerException was {innerException?.GetType().Name}");
            }
        }

        private void ThreadStartFunction()
        {
            var currentAssemblyDir = Path.GetDirectoryName(this.GetType().Assembly.Location)!;
            var wpfAppDirectory = Path.GetFullPath(Path.Combine(currentAssemblyDir, "../../../../src/tests/XAMLTools.WPFApp"));
            var themeFilesDirectory = Path.GetFullPath(Path.Combine(wpfAppDirectory, "Themes/Controls"));
            var themeFileName = Directory.GetFiles(themeFilesDirectory, "*.xaml", SearchOption.AllDirectories).FirstOrDefault();
            if (themeFileName is null)
            {
                throw new NullReferenceException();
            }

            var mutexName = "Local\\XamlTools_" + Path.GetFileName(themeFileName);

            MutexHelper.ExecuteLocked(() => { Thread.Sleep(3000); }, themeFileName, timeout: TimeSpan.FromSeconds(2));

        }
    }
}
