namespace XAMLTools.Helpers;

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

public class MutexHelper
{
    public static T ExecuteLocked<T>(Func<T> action, string file, [CallerArgumentExpression("action")] string? caller = null, TimeSpan? timeout = null, string errorMessage = "Another instance of this application blocked the concurrent execution.")
    {
        var mutexName = "Local\\XamlTools_" + Path.GetFileName(file);

        using var mutex = new Mutex(false, mutexName);

        try
        {
            if (mutex.WaitOne(timeout ?? TimeSpan.FromSeconds(10)) == false)
            {
                throw new TimeoutException(errorMessage);
            }

            return action();
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    public static void ExecuteLocked(Action action, string file, [CallerArgumentExpression("action")] string? caller = null, TimeSpan? timeout = null, string errorMessage = "Another instance of this application blocked the concurrent execution.")
    {
        var mutexName = "Local\\XamlTools_" + Path.GetFileName(file);

        using var mutex = new Mutex(false, mutexName);

        try
        {
            if (mutex.WaitOne(timeout ?? TimeSpan.FromSeconds(10)) == false)
            {
                throw new TimeoutException(errorMessage);
            }

            action();
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }
}