using System;
using System.Text.RegularExpressions;
using McpXLib.Enums;

namespace McTestPanel;

public static class DeviceAddressParser
{
    public static bool TryParse(string device, string address, out Prefix prefix, out string parsedAddress, out string error)
    {
        error = string.Empty;
        prefix = Prefix.M;
        parsedAddress = string.Empty;

        if (string.IsNullOrWhiteSpace(device))
        {
            error = "deviceが空です";
            return false;
        }

        if (!TryParsePrefix(device, out prefix))
        {
            error = $"未対応device: {device}";
            return false;
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            error = "addressが空です";
            return false;
        }

        parsedAddress = address.Trim();
        if (parsedAddress.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            parsedAddress = parsedAddress[2..];
        }

        if (!ValidateAddress(prefix, parsedAddress))
        {
            error = $"address不正: {address}";
            return false;
        }

        return true;
    }

    private static bool TryParsePrefix(string device, out Prefix prefix)
    {
        prefix = Prefix.M;
        device = device.Trim().ToUpperInvariant();

        return device switch
        {
            "M" => Assign(Prefix.M, out prefix),
            "X" => Assign(Prefix.X, out prefix),
            "Y" => Assign(Prefix.Y, out prefix),
            "B" => Assign(Prefix.B, out prefix),
            "CC" => Assign(Prefix.CC, out prefix),
            "CN" => Assign(Prefix.CN, out prefix),
            "CS" => Assign(Prefix.CS, out prefix),
            "D" => Assign(Prefix.D, out prefix),
            "DX" => Assign(Prefix.DX, out prefix),
            "DY" => Assign(Prefix.DY, out prefix),
            "F" => Assign(Prefix.F, out prefix),
            "L" => Assign(Prefix.L, out prefix),
            "SB" => Assign(Prefix.SB, out prefix),
            "SC" => Assign(Prefix.SC, out prefix),
            "SD" => Assign(Prefix.SD, out prefix),
            "SM" => Assign(Prefix.SM, out prefix),
            "SN" => Assign(Prefix.SN, out prefix),
            "SS" => Assign(Prefix.SS, out prefix),
            "W" => Assign(Prefix.W, out prefix),
            "SW" => Assign(Prefix.SW, out prefix),
            "R" => Assign(Prefix.R, out prefix),
            "S" => Assign(Prefix.S, out prefix),
            "TC" => Assign(Prefix.TC, out prefix),
            "TN" => Assign(Prefix.TN, out prefix),
            "TS" => Assign(Prefix.TS, out prefix),
            "V" => Assign(Prefix.V, out prefix),
            "Z" => Assign(Prefix.Z, out prefix),
            "ZR" => Assign(Prefix.ZR, out prefix),
            _ => false,
        };
    }

    private static bool Assign(Prefix value, out Prefix prefix)
    {
        prefix = value;
        return true;
    }

    private static bool ValidateAddress(Prefix prefix, string address)
    {
        if (IsHexDevice(prefix))
        {
            return Regex.IsMatch(address, @"^[0-9A-Fa-f]+$");
        }

        return uint.TryParse(address, out _);
    }

    private static bool IsHexDevice(Prefix prefix)
    {
        return prefix == Prefix.X ||
            prefix == Prefix.Y ||
            prefix == Prefix.B ||
            prefix == Prefix.W ||
            prefix == Prefix.SB ||
            prefix == Prefix.SW ||
            prefix == Prefix.DX ||
            prefix == Prefix.DY;
    }
}
