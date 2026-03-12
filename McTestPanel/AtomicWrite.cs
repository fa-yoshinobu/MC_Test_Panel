using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace McTestPanel;

public static class AtomicWrite
{
    public static void WriteAllLines(string path, IEnumerable<string> lines)
    {
        var temp = path + ".tmp";
        File.WriteAllLines(temp, lines);
        File.Copy(temp, path, true);
        File.Delete(temp);
    }

    public static void WriteAllText(string path, string text, Encoding encoding)
    {
        var temp = path + ".tmp";
        File.WriteAllText(temp, text, encoding);
        File.Copy(temp, path, true);
        File.Delete(temp);
    }
}
