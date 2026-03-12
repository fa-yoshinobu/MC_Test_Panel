using System;
using System.Windows.Forms;
using McpXLib.Enums;

namespace McTestPanel;

public sealed class DeviceBinding
{
    public DeviceBinding(DeviceDefinition definition, Prefix prefix, string address, Control container)
    {
        Definition = definition;
        Prefix = prefix;
        Address = address;
        Container = container;
    }

    public DeviceDefinition Definition { get; }
    public Prefix Prefix { get; }
    public string Address { get; }
    public Control Container { get; }

    public Action<bool>? UpdateBit { get; set; }
    public Action<short>? UpdateWord { get; set; }
    public Func<bool>? IsEditing { get; set; }
    public bool SuppressWrite { get; set; }
}
