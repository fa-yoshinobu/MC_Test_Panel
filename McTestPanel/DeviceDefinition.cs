namespace McTestPanel;

public sealed class DeviceDefinition
{
    public DeviceDefinition(
        string group,
        string name,
        DeviceType type,
        DeviceMode mode,
        string device,
        string address,
        short @const,
        string comment)
    {
        Group = group;
        Name = name;
        Type = type;
        Mode = mode;
        Device = device;
        Address = address;
        Const = @const;
        Comment = comment;
    }

    public string Group { get; }
    public string Name { get; }
    public DeviceType Type { get; }
    public DeviceMode Mode { get; }
    public string Device { get; }
    public string Address { get; }
    public short Const { get; }
    public string Comment { get; }
}
