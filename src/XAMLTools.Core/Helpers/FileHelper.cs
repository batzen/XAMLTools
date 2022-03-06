namespace XAMLTools.Helpers;

using System;
using System.IO;
using System.Text;
using System.Threading;

public class FileHelper
{
    private const int BufferSize = 32768; // 32 Kilobytes

    public static string ReadAllTextSharedWithRetry(string file, ushort retries = 5)
    {
        for (var i = 0; i < retries; i++)
        {
            try
            {
                return ReadAllTextShared(file);
            }
            catch (IOException)
            {
                if (i == retries - 1)
                {
                    throw;
                }

                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        throw new IOException($"File \"{file}\" could not be read.");
    }

    public static string ReadAllTextShared(string file)
    {
        Stream? stream = null;
        try
        {
            stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize);

            using (var textReader = new StreamReader(stream, Encoding.UTF8))
            {
                stream = null;
                return textReader.ReadToEnd();
            }
        }
        finally
        {
            stream?.Dispose();
        }
    }
}