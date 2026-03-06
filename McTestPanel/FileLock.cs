using System;
using System.IO;
using System.Threading;

namespace McTestPanel;

public static class FileLock
{
    public static IDisposable Acquire(string targetPath)
    {
        var lockPath = targetPath + ".lock";
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? AppContext.BaseDirectory);

        FileStream? stream = null;
        var start = DateTime.UtcNow;
        while ((DateTime.UtcNow - start).TotalMilliseconds < 5000)
        {
            try
            {
                stream = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                break;
            }
            catch (IOException)
            {
                Thread.Sleep(25);
            }
        }

        stream ??= new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        return stream;
    }
}
