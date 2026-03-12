using System;
using McpXLib;
using McpXLib.Enums;

namespace McTestPanel;

public static class McpXFactory
{
    public static PlcClient Create(ConnectionSettings settings)
    {
        var password = settings.UsePassword && !string.IsNullOrWhiteSpace(settings.Password)
            ? settings.Password
            : null;
        var client = new McpX(
            settings.Ip,
            settings.Port,
            password,
            settings.IsAscii,
            settings.IsUdp,
            settings.RequestFrame,
            (ushort)Math.Clamp(settings.TimeoutMs, 100, ushort.MaxValue));

        return new PlcClient(client);
    }
}
