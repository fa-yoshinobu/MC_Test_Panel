using System;
using System.Drawing;
using System.Windows.Forms;
using McpXLib.Enums;

namespace McTestPanel;

public sealed class ConnectionSettingsForm : Form
{
    private readonly TextBox _ipBox;
    private readonly NumericUpDown _portBox;
    private readonly NumericUpDown _timeoutBox;
    private readonly CheckBox _asciiBox;
    private readonly CheckBox _udpBox;
    private readonly ComboBox _frameBox;
    private readonly TextBox _passwordBox;
    private readonly CheckBox _usePasswordBox;

    public ConnectionSettings Settings { get; private set; }

    public ConnectionSettingsForm(ConnectionSettings current)
    {
        Settings = current;

        Text = "接続設定";
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        MinimumSize = new Size(460, 520);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        AutoScaleMode = AutoScaleMode.Font;
        AutoScroll = true;
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = true;
        Shown += (_, __) => EnsureVisible();

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 9,
            Padding = new Padding(12),
            AutoSize = true,
            AutoScroll = true,
        };

        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _ipBox = new TextBox { Text = current.Ip, Dock = DockStyle.Fill, MinimumSize = new Size(260, 0) };
        _portBox = new NumericUpDown { Minimum = 1, Maximum = 65535, Value = current.Port };
        _timeoutBox = new NumericUpDown { Minimum = 100, Maximum = 60000, Value = current.TimeoutMs };
        _asciiBox = new CheckBox { Text = "ASCII", Checked = current.IsAscii };
        _udpBox = new CheckBox { Text = "UDP", Checked = current.IsUdp };
        _frameBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        _frameBox.Items.AddRange(Enum.GetNames<RequestFrame>());
        _frameBox.SelectedItem = current.RequestFrame.ToString();
        _usePasswordBox = new CheckBox { Text = "Password", Checked = current.UsePassword, AutoSize = true };
        _passwordBox = new TextBox
        {
            Text = current.Password,
            UseSystemPasswordChar = true,
            Enabled = current.UsePassword,
            Dock = DockStyle.Fill,
            MinimumSize = new Size(260, 0),
        };
        _usePasswordBox.CheckedChanged += (_, __) =>
        {
            _passwordBox.Enabled = _usePasswordBox.Checked;
        };

        table.Controls.Add(new Label { Text = "IPアドレス", AutoSize = true }, 0, 0);
        table.Controls.Add(_ipBox, 1, 0);
        table.Controls.Add(new Label { Text = "ポート", AutoSize = true }, 0, 1);
        table.Controls.Add(_portBox, 1, 1);
        table.Controls.Add(new Label { Text = "タイムアウト(ms)", AutoSize = true }, 0, 2);
        table.Controls.Add(_timeoutBox, 1, 2);
        table.Controls.Add(new Label { Text = "フレーム", AutoSize = true }, 0, 3);
        table.Controls.Add(_frameBox, 1, 3);
        _usePasswordBox.Text = "パスワード";
        table.Controls.Add(_usePasswordBox, 1, 4);
        table.Controls.Add(_passwordBox, 1, 5);
        table.Controls.Add(_asciiBox, 1, 6);
        table.Controls.Add(_udpBox, 1, 7);

        var buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            Padding = new Padding(12, 12, 12, 24),
            Height = 64,
        };

        var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Size = new Size(90, 30) };
        var cancelButton = new Button { Text = "キャンセル", DialogResult = DialogResult.Cancel, Size = new Size(90, 30) };
        okButton.Click += (_, __) => ApplySettings();

        buttonPanel.Controls.Add(okButton);
        buttonPanel.Controls.Add(cancelButton);

        Controls.Add(table);
        Controls.Add(buttonPanel);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    private void EnsureVisible()
    {
        var area = Screen.PrimaryScreen?.WorkingArea ?? Screen.FromControl(this).WorkingArea;
        var x = Math.Max(area.Left, Math.Min(Left, area.Right - Width));
        var y = Math.Max(area.Top, Math.Min(Top, area.Bottom - Height));
        Location = new Point(x, y);
    }

    private void ApplySettings()
    {
        Settings = new ConnectionSettings
        {
            Ip = _ipBox.Text.Trim(),
            Port = (int)_portBox.Value,
            TimeoutMs = (int)_timeoutBox.Value,
            UsePassword = _usePasswordBox.Checked,
            Password = _passwordBox.Text,
            IsAscii = _asciiBox.Checked,
            IsUdp = _udpBox.Checked,
            RequestFrame = Enum.TryParse<RequestFrame>(_frameBox.SelectedItem?.ToString(), true, out var frame)
                ? frame
                : RequestFrame.E3,
        };
    }
}
