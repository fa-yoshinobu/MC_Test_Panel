namespace McTestPanel;

public sealed class DeviceDefinitionRow
{
    public string Group { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Bit";
    public string Mode { get; set; } = "Momentary";
    public string Device { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Const { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
}
