using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using McpXLib.Enums;

namespace McTestPanel;

public sealed class ConnectionSettings
{
    public string Ip { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 5007;
    public string Password { get; set; } = string.Empty;
    public bool UsePassword { get; set; }
    public bool IsAscii { get; set; }
    public bool IsUdp { get; set; }
    public RequestFrame RequestFrame { get; set; } = RequestFrame.E3;
    public int TimeoutMs { get; set; } = 1000;
    public bool AlwaysOnTop { get; set; }

    public static ConnectionSettings Default() => new();

    public static string DefaultPath()
    {
        return Path.Combine(AppContext.BaseDirectory, "connection.csv");
    }

    public static ConnectionSettings Load(string path)
    {
        var settings = Default();
        foreach (var line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
            {
                continue;
            }

            var parts = line.Split(',', 2, StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
            {
                continue;
            }

            var key = parts[0].Trim().ToLowerInvariant();
            var value = parts[1].Trim();

            switch (key)
            {
                case "ip":
                    settings.Ip = value;
                    break;
                case "port":
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port))
                        settings.Port = port;
                    break;
                case "password":
                    settings.Password = value;
                    break;
                case "usepassword":
                    if (bool.TryParse(value, out var usePassword))
                        settings.UsePassword = usePassword;
                    break;
                case "isascii":
                    if (bool.TryParse(value, out var isAscii))
                        settings.IsAscii = isAscii;
                    break;
                case "isudp":
                    if (bool.TryParse(value, out var isUdp))
                        settings.IsUdp = isUdp;
                    break;
                case "requestframe":
                    if (Enum.TryParse<RequestFrame>(value, true, out var frame))
                        settings.RequestFrame = frame;
                    break;
                case "timeoutms":
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var timeout))
                        settings.TimeoutMs = timeout;
                    break;
                case "alwaysontop":
                    if (bool.TryParse(value, out var alwaysOnTop))
                        settings.AlwaysOnTop = alwaysOnTop;
                    break;
            }
        }

        return settings;
    }

    public void Save(string path)
    {
        using var _ = FileLock.Acquire(path);
        var lines = new List<string>
        {
            "ip," + Ip,
            "port," + Port,
            "usePassword," + UsePassword,
            "password," + Password,
            "isAscii," + IsAscii,
            "isUdp," + IsUdp,
            "requestFrame," + RequestFrame,
            "timeoutMs," + TimeoutMs,
            "alwaysOnTop," + AlwaysOnTop,
        };

        AtomicWrite.WriteAllLines(path, lines);
    }
}
