using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualBasic.FileIO;

namespace McTestPanel;

public sealed class DeviceCsvLoadResult
{
    public List<DeviceDefinition> Definitions { get; } = new();
    public List<string> Errors { get; } = new();
}

public static class DeviceCsvLoader
{
    private static readonly string[] RequiredColumns =
    {
        "group",
        "name",
        "type",
        "mode",
        "device",
        "address",
    };

    public static string DefaultPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "devices.csv");
    }

    public static DeviceCsvLoadResult Load(string path)
    {
        var result = new DeviceCsvLoadResult();

        if (!File.Exists(path))
        {
            result.Errors.Add("CSVが存在しません");
            return result;
        }

        using var parser = new TextFieldParser(path);
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;

        if (parser.EndOfData)
        {
            result.Errors.Add("CSVが空です");
            return result;
        }

        var header = parser.ReadFields() ?? Array.Empty<string>();
        if (header.Length == 0 || header.All(string.IsNullOrWhiteSpace))
        {
            result.Errors.Add("ヘッダ行が空です");
            return result;
        }
        var map = BuildHeaderMap(header);

        foreach (var required in RequiredColumns)
        {
            if (!map.ContainsKey(required))
            {
                result.Errors.Add($"必須列がありません: {required}");
            }
        }

        if (result.Errors.Count > 0)
        {
            return result;
        }

        while (!parser.EndOfData)
        {
            var startLine = parser.LineNumber + 1;
            var fields = parser.ReadFields();
            var endLine = parser.LineNumber;
            var rowIndex = startLine == endLine ? startLine.ToString() : $"{startLine}-{endLine}";
            if (fields == null || fields.Length == 0)
            {
                continue;
            }

            if (IsRowEmpty(fields))
            {
                continue;
            }

            try
            {
                var group = GetField(fields, map, "group");
                var name = GetField(fields, map, "name");
                var typeText = GetField(fields, map, "type");
                var modeText = GetField(fields, map, "mode");
                var device = GetField(fields, map, "device");
                var address = GetField(fields, map, "address");
                var constText = GetOptionalField(fields, map, "const");
                var comment = GetOptionalField(fields, map, "comment");

                if (!Enum.TryParse<DeviceType>(typeText, true, out var type))
                {
                    result.Errors.Add($"type不正: {name} (列 type, 行 {rowIndex})");
                    continue;
                }

                if (!Enum.TryParse<DeviceMode>(modeText, true, out var mode))
                {
                    result.Errors.Add($"mode不正: {name} (列 mode, 行 {rowIndex})");
                    continue;
                }

                if (!TryParseConst(constText, out var constValue))
                {
                    result.Errors.Add($"const不正: {name} (列 const, 行 {rowIndex})");
                    continue;
                }

                result.Definitions.Add(new DeviceDefinition(
                    group,
                    name,
                    type,
                    mode,
                    device,
                    address,
                    constValue,
                    comment));
            }
            catch (Exception ex)
            {
                result.Errors.Add($"行解析エラー(行 {rowIndex}): {ex.Message}");
            }
        }

        return result;
    }

    private static Dictionary<string, int> BuildHeaderMap(string[] header)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < header.Length; i++)
        {
            var key = header[i].Trim().ToLowerInvariant();
            if (!map.ContainsKey(key) && key.Length > 0)
            {
                map[key] = i;
            }
        }

        return map;
    }

    private static string GetField(string[] fields, Dictionary<string, int> map, string key)
    {
        if (!map.TryGetValue(key, out var index))
        {
            return string.Empty;
        }

        return index < fields.Length ? fields[index].Trim() : string.Empty;
    }

    private static string GetOptionalField(string[] fields, Dictionary<string, int> map, string key)
    {
        return map.TryGetValue(key, out var index) && index < fields.Length
            ? fields[index].Trim()
            : string.Empty;
    }

    private static bool TryParseConst(string text, out short value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        text = text.Trim();
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            text = text[2..];
            return short.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out value);
        }

        if (text.IndexOfAny("ABCDEFabcdef".ToCharArray()) >= 0)
        {
            return short.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out value);
        }

        return short.TryParse(text, out value);
    }

    private static bool IsRowEmpty(string[] fields)
    {
        foreach (var field in fields)
        {
            if (!string.IsNullOrWhiteSpace(field))
            {
                return false;
            }
        }

        return true;
    }
}
