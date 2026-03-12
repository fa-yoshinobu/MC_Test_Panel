using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace McTestPanel;

public static class DeviceCsvWriter
{
    private static readonly string[] Columns =
    {
        "group",
        "name",
        "type",
        "mode",
        "device",
        "address",
        "const",
        "comment",
    };

    public static void Save(string path, IEnumerable<DeviceDefinitionRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', Columns));

        foreach (var row in rows)
        {
            var fields = new[]
            {
                row.Group,
                row.Name,
                row.Type,
                row.Mode,
                row.Device,
                row.Address,
                row.Const,
                row.Comment,
            };

            sb.AppendLine(string.Join(',', Escape(fields)));
        }

        using var _ = FileLock.Acquire(path);
        AtomicWrite.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static IEnumerable<string> Escape(IEnumerable<string?> fields)
    {
        foreach (var field in fields)
        {
            var value = field ?? string.Empty;
            if (value.Contains('"'))
            {
                value = value.Replace("\"", "\"\"");
            }

            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            {
                value = '"' + value + '"';
            }

            yield return value;
        }
    }
}
