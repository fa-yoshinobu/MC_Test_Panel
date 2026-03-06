using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using McpXLib.Enums;

namespace McTestPanel;

public partial class MainForm : Form
{
    private const int RowHeight = 52;
    private const int ControlHeight = 34;
    private const int TextHeight = 28;
    private const int TopPadding = 9;
    private const int NameColumnWidth = 260;
    private const int NameColumnX = 8;
    private const int ActionColumnWidth = 140;
    private const int ActionControlWidth = 120;
    private const int ValueColumnWidth = 140;
    private const int ValueControlWidth = 120;
    private const int ModeColumnWidth = 140;
    private const int AddressColumnWidth = 140;
    private const int HeaderHeight = 28;
    private const int ColumnGap = 16;
    private const int NameMaxWidth = 520;
    private readonly List<DeviceBinding> _bindings = new();
    private PlcClient? _client;
    private ConnectionSettings _connectionSettings = ConnectionSettings.Default();
    private string? _deviceCsvPath;
    private RadixMode _radixMode = RadixMode.Dec;
    private int _pollIntervalMs = 500;
    private readonly System.Windows.Forms.Timer _pollTimer;
    private bool _pollInProgress;
    private int _consecutiveReadErrors;
    private const int MaxConsecutiveReadErrors = 5;
    private DateTime _lastErrorTime = DateTime.MinValue;
    private readonly System.Windows.Forms.Timer _autoRetryTimer;
    private bool _autoStopped;
    private bool _isConnecting;
    private readonly ToolTip _toolTip = new();
    private LogWindow? _logWindow;
    private Panel? _headerPanel;

    public MainForm()
    {
        InitializeComponent();
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? Icon;
        StartPosition = FormStartPosition.CenterScreen;

        _toolTip.ShowAlways = true;
        devicePanel.AutoScroll = true;
        devicePanel.HorizontalScroll.Enabled = true;
        devicePanel.HorizontalScroll.Visible = true;
        devicePanel.VerticalScroll.Enabled = true;
        devicePanel.VerticalScroll.Visible = true;

        _pollTimer = new System.Windows.Forms.Timer();
        _pollTimer.Interval = _pollIntervalMs;
        _pollTimer.Tick += async (_, __) => await PollOnceAsync();

        _autoRetryTimer = new System.Windows.Forms.Timer();
        _autoRetryTimer.Interval = 3000;
        _autoRetryTimer.Tick += async (_, __) =>
        {
            _autoRetryTimer.Stop();
            if (_autoStopped && !IsDisposed)
            {
                await StartCommunicationAsync();
            }
        };

        groupList.SelectedIndexChanged += (_, __) => RenderSelectedGroup();
        devicePanel.SizeChanged += (_, __) => UpdateRowWidths();
        SizeChanged += (_, __) => AdjustGroupPanelWidth();

        openDeviceCsvItem.Click += (_, __) => LoadDeviceCsvFromDialog();
        reloadDeviceCsvItem.Click += (_, __) => ReloadDeviceCsv();
        exitItem.Click += (_, __) => Close();

        connectionSettingsItem.Click += (_, __) => OpenConnectionSettings();
        deviceSettingsItem.Click += (_, __) => OpenDeviceSettings();
        commStartItem.Click += async (_, __) => await StartCommunicationAsync();
        commStopItem.Click += (_, __) => StopCommunication();
        logWindowItem.Click += (_, __) => ToggleLogWindow();
        aboutItem.Click += (_, __) => ShowAbout();

        radixDecItem.Click += (_, __) => SetRadix(RadixMode.Dec);
        radixHexItem.Click += (_, __) => SetRadix(RadixMode.Hex);

        interval100Item.Click += (_, __) => SetPollInterval(100);
        interval200Item.Click += (_, __) => SetPollInterval(200);
        interval500Item.Click += (_, __) => SetPollInterval(500);
        interval1000Item.Click += (_, __) => SetPollInterval(1000);
        alwaysOnTopItem.Click += (_, __) => ToggleAlwaysOnTop();

        Shown += async (_, __) => await InitializeAsync();
        FormClosing += (_, __) => StopCommunication();
    }

    private async Task InitializeAsync()
    {
        UpdateStatus("起動中...");
        EnsureDefaultFiles();
        LoadConnectionSettings();
        AdjustGroupPanelWidth();
        await StartCommunicationAsync();
        TryLoadDefaultDeviceCsv();
        ApplyAlwaysOnTop();
        ShowLogWindowOnStartup();
        UpdateStatus("準備完了");
    }

    private void LoadConnectionSettings()
    {
        var path = ConnectionSettings.DefaultPath();
        if (File.Exists(path))
        {
            _connectionSettings = ConnectionSettings.Load(path);
        }
    }


    private async Task ConnectAsync()
    {
        try
        {
            if (_isConnecting) return;
            _isConnecting = true;

            if (_client != null)
            {
                try { _client.Close(); } catch { }
            }

            _client = McpXFactory.Create(_connectionSettings);
            _client.Open();

            connectionStatusLabel.Text = $"接続: OK ({_connectionSettings.Ip}:{_connectionSettings.Port})";
            connectionStatusLabel.ForeColor = Color.DarkGreen;
        }
        catch (Exception ex)
        {
            _client = null;
            connectionStatusLabel.Text = "接続: 失敗";
            connectionStatusLabel.ForeColor = Color.DarkRed;
            UpdateStatus($"接続失敗: {ex.Message} (設定→接続設定を確認)", true);
        }
        finally
        {
            _isConnecting = false;
        }

        await Task.CompletedTask;
    }

    private async Task StartCommunicationAsync()
    {
        _autoRetryTimer.Stop();
        await ConnectAsync();
        if (_client != null)
        {
            StartPolling();
            _autoStopped = false;
            UpdateStatus("通信開始");
        }
    }

    private void StopCommunication(bool autoStop = false)
    {
        _pollTimer.Stop();
        _autoRetryTimer.Stop();
        _autoStopped = autoStop;
        if (_client != null)
        {
            try { _client.Close(); } catch { }
            _client = null;
        }

        connectionStatusLabel.Text = "接続: 停止";
        connectionStatusLabel.ForeColor = Color.DarkRed;
        UpdateStatus("通信停止");
    }

    private void TryLoadDefaultDeviceCsv()
    {
        var path = DeviceCsvLoader.DefaultPath();
        if (File.Exists(path))
        {
            _deviceCsvPath = path;
            LoadDeviceCsv(path);
        }
    }

    private void LoadDeviceCsvFromDialog()
    {
        using var dialog = new OpenFileDialog();
        dialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
        dialog.Title = "デバイスCSVを開く";

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _deviceCsvPath = dialog.FileName;
            LoadDeviceCsv(_deviceCsvPath);
        }
    }

    private void ReloadDeviceCsv()
    {
        if (!string.IsNullOrWhiteSpace(_deviceCsvPath) && File.Exists(_deviceCsvPath))
        {
            LoadDeviceCsv(_deviceCsvPath);
        }
    }

    private void LoadDeviceCsv(string path)
    {
        var result = DeviceCsvLoader.Load(path);
        if (result.Errors.Count > 0)
        {
            UpdateStatus($"CSVエラー: {string.Join(" / ", result.Errors)}", true);
        }

        _bindings.Clear();
        devicePanel.Controls.Clear();
        ClearHeader();
        groupList.Items.Clear();

        foreach (var def in result.Definitions)
        {
            if (!DeviceAddressParser.TryParse(def.Device, def.Address, out var prefix, out var address, out var error))
            {
                UpdateStatus($"アドレスエラー: {def.Name} {error}", true);
                continue;
            }

            var binding = CreateBinding(def, prefix, address);
            _bindings.Add(binding);
        }

        foreach (var group in _bindings.Select(b => b.Definition.Group).Distinct())
        {
            groupList.Items.Add(group);
        }

        if (groupList.Items.Count > 0)
        {
            groupList.SelectedIndex = 0;
        }

        fileStatusLabel.Text = $"CSV: {Path.GetFileName(path)}";
        UpdateStatus("CSV読み込み完了");
        UpdateRowWidths();
        StartPolling();
    }

    private DeviceBinding CreateBinding(DeviceDefinition def, Prefix prefix, string address)
    {
        var row = new Panel
        {
            Height = RowHeight,
            Width = Math.Max(200, devicePanel.ClientSize.Width - 24),
            Margin = new Padding(6, 4, 6, 4),
            BackColor = Color.WhiteSmoke,
        };

        var nameLabel = new Label
        {
            Text = def.Name,
            AutoSize = false,
            Width = NameColumnWidth,
            Height = ControlHeight,
            Location = new Point(NameColumnX, TopPadding),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        nameLabel.Tag = "name";
        row.Controls.Add(nameLabel);
        var nameWidth = TextRenderer.MeasureText(def.Name, nameLabel.Font).Width + 8;
        if (nameWidth > nameLabel.Width)
        {
            AttachToolTip(nameLabel, def.Name);
        }

        var modeLabel = new Label
        {
            Text = GetModeBadge(def.Mode),
            AutoSize = false,
            Width = ModeColumnWidth,
            Height = ControlHeight,
            Location = new Point(NameColumnX + NameColumnWidth + ColumnGap + ActionColumnWidth + ColumnGap + ValueColumnWidth + ColumnGap, TopPadding),
            ForeColor = Color.White,
            BackColor = GetModeColor(def.Mode),
            TextAlign = ContentAlignment.MiddleCenter,
        };
        modeLabel.Tag = "mode";
        row.Controls.Add(modeLabel);

        var addressLabel = new Label
        {
            Text = $"{def.Device}{def.Address}",
            AutoSize = false,
            Width = AddressColumnWidth,
            Height = ControlHeight,
            Location = new Point(modeLabel.Location.X + ModeColumnWidth + ColumnGap, TopPadding),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.DimGray,
        };
        addressLabel.Tag = "address";
        row.Controls.Add(addressLabel);

        var binding = new DeviceBinding(def, prefix, address, row);

        switch (def.Type)
        {
            case DeviceType.Bit:
                CreateBitControl(binding, row, NameColumnX + NameColumnWidth + ColumnGap, NameColumnX + NameColumnWidth + ColumnGap + ActionColumnWidth + ColumnGap);
                break;
            case DeviceType.Word:
                CreateWordControl(binding, row, NameColumnX + NameColumnWidth + ColumnGap, NameColumnX + NameColumnWidth + ColumnGap + ActionColumnWidth + ColumnGap);
                break;
        }

        return binding;
    }

    private void CreateBitControl(DeviceBinding binding, Panel row, int actionX, int valueX)
    {
        var valueBox = new TextBox
        {
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.Gainsboro,
            Width = ValueControlWidth,
            Height = TextHeight,
        };
        valueBox.Tag = "value";
        valueBox.Location = new Point(CenterX(valueX, ValueColumnWidth, valueBox.Width), TopPadding + 3);
        row.Controls.Add(valueBox);

        switch (binding.Definition.Mode)
        {
            case DeviceMode.Momentary:
                var momentary = new Button
                {
                    Text = "PUSH",
                    Size = new Size(ActionControlWidth, ControlHeight),
                };
                momentary.Tag = "action";
                ApplyPressFeedback(momentary);
                momentary.Location = new Point(CenterX(actionX, ActionColumnWidth, momentary.Width), TopPadding);

                momentary.MouseDown += async (_, __) => await WriteBitAsync(binding, true);
                momentary.MouseUp += async (_, __) => await WriteBitAsync(binding, false);
                binding.UpdateBit = value =>
                {
                    valueBox.Text = value ? "ON" : "OFF";
                    momentary.BackColor = value ? Color.LightGreen : SystemColors.Control;
                    momentary.FlatStyle = FlatStyle.Standard;
                };
                row.Controls.Add(momentary);
                break;

            case DeviceMode.Alternate:
                var toggle = new CheckBox
                {
                    Appearance = Appearance.Button,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Text = "OFF",
                    Size = new Size(ActionControlWidth, ControlHeight),
                };
                toggle.Tag = "action";
                toggle.FlatStyle = FlatStyle.Standard;
                toggle.Location = new Point(CenterX(actionX, ActionColumnWidth, toggle.Width), TopPadding);

                toggle.CheckedChanged += async (_, __) =>
                {
                    if (binding.SuppressWrite) return;
                    await WriteBitAsync(binding, toggle.Checked);
                };

                binding.UpdateBit = value =>
                {
                    binding.SuppressWrite = true;
                    toggle.Checked = value;
                    toggle.Text = value ? "ON" : "OFF";
                    valueBox.Text = value ? "ON" : "OFF";
                    toggle.BackColor = value ? Color.LightGreen : SystemColors.Control;
                    toggle.FlatStyle = FlatStyle.Standard;
                    binding.SuppressWrite = false;
                };

                row.Controls.Add(toggle);
                break;

            case DeviceMode.Lamp:
                var lamp = new LampControl
                {
                    Size = new Size(26, 26),
                };
                lamp.Tag = "action";
                lamp.Location = new Point(CenterX(actionX, ActionColumnWidth, lamp.Width), TopPadding);

                binding.UpdateBit = value =>
                {
                    lamp.IsOn = value;
                    valueBox.Text = value ? "ON" : "OFF";
                };
                row.Controls.Add(lamp);
                break;
        }
    }

    private void CreateWordControl(DeviceBinding binding, Panel row, int actionX, int valueX)
    {
        var valueBox = new TextBox
        {
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.Gainsboro,
            Width = ValueControlWidth,
            Height = TextHeight,
        };
        valueBox.Tag = "value";
        valueBox.Location = new Point(CenterX(valueX, ValueColumnWidth, valueBox.Width), TopPadding + 3);
        row.Controls.Add(valueBox);

        switch (binding.Definition.Mode)
        {
            case DeviceMode.Value:
                var input = new TextBox
                {
                    Width = ActionControlWidth,
                    Height = TextHeight,
                };
                input.Tag = "action";
                input.Location = new Point(CenterX(actionX, ActionColumnWidth, input.Width), TopPadding + 3);

                input.KeyDown += async (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        e.SuppressKeyPress = true;
                        if (TryParseWordInput(input.Text, out var value, out var error))
                        {
                            await WriteWordAsync(binding, value);
                        }
                        else
                        {
                            UpdateStatus(error, true);
                        }
                    }
                };

                binding.IsEditing = () => input.Focused;
                binding.UpdateWord = value =>
                {
                    valueBox.Text = FormatWordValue(value);
                    if (!input.Focused)
                    {
                        input.Text = FormatWordValue(value);
                    }
                };

                row.Controls.Add(input);
                break;

            case DeviceMode.SetConst:
                var setButton = new Button
                {
                    Text = $"SET {binding.Definition.Const}",
                    Size = new Size(ActionControlWidth, ControlHeight),
                };
                setButton.Tag = "action";
                ApplyPressFeedback(setButton);
                setButton.Location = new Point(CenterX(actionX, ActionColumnWidth, setButton.Width), TopPadding);

                setButton.Click += async (_, __) => await WriteWordAsync(binding, binding.Definition.Const);
                binding.UpdateWord = value => valueBox.Text = FormatWordValue(value);
                row.Controls.Add(setButton);
                break;

            case DeviceMode.Inc:
                var incButton = new Button
                {
                    Text = "+",
                    Size = new Size(ActionControlWidth, ControlHeight),
                };
                incButton.Tag = "action";
                ApplyPressFeedback(incButton);
                incButton.Location = new Point(CenterX(actionX, ActionColumnWidth, incButton.Width), TopPadding);

                incButton.Click += async (_, __) => await IncrementWordAsync(binding, +1);
                binding.UpdateWord = value => valueBox.Text = FormatWordValue(value);
                row.Controls.Add(incButton);
                break;

            case DeviceMode.Dec:
                var decButton = new Button
                {
                    Text = "-",
                    Size = new Size(ActionControlWidth, ControlHeight),
                };
                decButton.Tag = "action";
                ApplyPressFeedback(decButton);
                decButton.Location = new Point(CenterX(actionX, ActionColumnWidth, decButton.Width), TopPadding);

                decButton.Click += async (_, __) => await IncrementWordAsync(binding, -1);
                binding.UpdateWord = value => valueBox.Text = FormatWordValue(value);
                row.Controls.Add(decButton);
                break;

            case DeviceMode.Display:
                binding.UpdateWord = value => valueBox.Text = FormatWordValue(value);
                break;
        }
    }

    private void RenderSelectedGroup()
    {
        devicePanel.SuspendLayout();
        devicePanel.Controls.Clear();
        ClearHeader();

        var group = groupList.SelectedItem?.ToString();
        if (group == null)
        {
            devicePanel.ResumeLayout();
            return;
        }

        var header = GetOrCreateHeaderPanel();
        devicePanel.Controls.Add(header);

        foreach (var binding in _bindings.Where(b => b.Definition.Group == group))
        {
            devicePanel.Controls.Add(binding.Container);
        }

        ApplyGroupLayout(group);
        UpdateRowWidths();
        devicePanel.ResumeLayout();
    }

    private void UpdateRowWidths()
    {
        var width = Math.Max(200, devicePanel.ClientSize.Width - 24);
        foreach (var binding in _bindings)
        {
            binding.Container.Width = width;
        }

        if (_headerPanel != null)
        {
            _headerPanel.Width = width;
        }
    }

    private void ApplyGroupLayout(string group)
    {
        var groupBindings = _bindings.Where(b => b.Definition.Group == group).ToList();
        if (groupBindings.Count == 0) return;

        var maxNameWidth = NameColumnWidth;
        foreach (var binding in groupBindings)
        {
            var nameLabel = FindTaggedControl(binding.Container, "name") as Label;
            if (nameLabel == null) continue;
            var measured = TextRenderer.MeasureText(nameLabel.Text, nameLabel.Font).Width + 8;
            maxNameWidth = Math.Max(maxNameWidth, measured);
        }

        maxNameWidth = Math.Min(NameMaxWidth, maxNameWidth);
        var actionX = NameColumnX + maxNameWidth + ColumnGap;
        var valueX = actionX + ActionColumnWidth + ColumnGap;
        var modeX = valueX + ValueColumnWidth + ColumnGap;
        var addressX = modeX + ModeColumnWidth + ColumnGap;

        foreach (var binding in groupBindings)
        {
            var nameLabel = FindTaggedControl(binding.Container, "name") as Label;
            if (nameLabel != null)
            {
                nameLabel.Width = maxNameWidth;
            }

            var modeLabel = FindTaggedControl(binding.Container, "mode") as Label;
            if (modeLabel != null)
            {
                modeLabel.Location = new Point(modeX, modeLabel.Location.Y);
            }

            var addressLabel = FindTaggedControl(binding.Container, "address") as Label;
            if (addressLabel != null)
            {
                addressLabel.Location = new Point(addressX, addressLabel.Location.Y);
            }

            var actionControl = FindTaggedControl(binding.Container, "action");
            if (actionControl != null)
            {
                actionControl.Location = new Point(
                    CenterX(actionX, ActionColumnWidth, actionControl.Width),
                    actionControl.Location.Y);
            }

            var valueControl = FindTaggedControl(binding.Container, "value");
            if (valueControl != null)
            {
                valueControl.Location = new Point(
                    CenterX(valueX, ValueColumnWidth, valueControl.Width),
                    valueControl.Location.Y);
            }
        }

        UpdateHeaderLayout(maxNameWidth);
        UpdateScrollSize(addressX + AddressColumnWidth);
    }

    private static Control? FindTaggedControl(Control container, string tag)
    {
        foreach (Control control in container.Controls)
        {
            if (control.Tag as string == tag)
            {
                return control;
            }
        }

        return null;
    }

    private void UpdateScrollSize(int contentRight)
    {
        var rightPadding = 24;
        var panelContentWidth = contentRight + rightPadding;
        devicePanel.AutoScrollMinSize = new Size(panelContentWidth, 0);
    }

    private Panel GetOrCreateHeaderPanel()
    {
        if (_headerPanel != null)
        {
            _headerPanel.Visible = true;
            return _headerPanel;
        }

        var header = new Panel
        {
            Height = HeaderHeight,
            Width = Math.Max(200, devicePanel.ClientSize.Width - 24),
            Margin = new Padding(6, 4, 6, 4),
            BackColor = Color.Gainsboro,
            Tag = "header",
        };

        header.Controls.Add(CreateHeaderLabel("名称", "header_name"));
        header.Controls.Add(CreateHeaderLabel("操作", "header_action"));
        header.Controls.Add(CreateHeaderLabel("値", "header_value"));
        header.Controls.Add(CreateHeaderLabel("モード", "header_mode"));
        header.Controls.Add(CreateHeaderLabel("アドレス", "header_address"));

        _headerPanel = header;
        return header;
    }

    private Label CreateHeaderLabel(string text, string tag)
    {
        return new Label
        {
            Text = text,
            Tag = tag,
            AutoSize = false,
            Height = HeaderHeight,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(Font, FontStyle.Bold),
        };
    }

    private void UpdateHeaderLayout(int nameWidth)
    {
        if (_headerPanel == null)
        {
            return;
        }

        var actionX = NameColumnX + nameWidth + ColumnGap;
        var valueX = actionX + ActionColumnWidth + ColumnGap;
        var modeX = valueX + ValueColumnWidth + ColumnGap;
        var addressX = modeX + ModeColumnWidth + ColumnGap;

        foreach (Control control in _headerPanel.Controls)
        {
            var label = control as Label;
            if (label == null)
            {
                continue;
            }

            switch (control.Tag as string)
            {
                case "header_name":
                    label.Width = nameWidth;
                    label.Location = new Point(NameColumnX, 0);
                    break;
                case "header_action":
                    label.Width = ActionColumnWidth;
                    label.Location = new Point(actionX, 0);
                    label.TextAlign = ContentAlignment.MiddleCenter;
                    break;
                case "header_value":
                    label.Width = ValueColumnWidth;
                    label.Location = new Point(valueX, 0);
                    label.TextAlign = ContentAlignment.MiddleCenter;
                    break;
                case "header_mode":
                    label.Width = ModeColumnWidth;
                    label.Location = new Point(modeX, 0);
                    label.TextAlign = ContentAlignment.MiddleCenter;
                    break;
                case "header_address":
                    label.Width = AddressColumnWidth;
                    label.Location = new Point(addressX, 0);
                    label.TextAlign = ContentAlignment.MiddleLeft;
                    break;
            }
        }
    }

    private void ClearHeader()
    {
        if (_headerPanel == null)
        {
            return;
        }

        _headerPanel.Visible = false;
        _headerPanel.Parent?.Controls.Remove(_headerPanel);
    }

    private void AdjustGroupPanelWidth()
    {
        var target = (int)Math.Max(120, ClientSize.Width * 0.25);
        if (splitContainer.SplitterDistance != target)
        {
            splitContainer.SplitterDistance = target;
        }
    }

    private void StartPolling()
    {
        if (_bindings.Count == 0 || _client == null)
        {
            return;
        }

        _pollTimer.Interval = _pollIntervalMs;
        _pollTimer.Start();
    }

    private async Task PollOnceAsync()
    {
        if (_pollInProgress || _client == null || !_pollTimer.Enabled)
        {
            return;
        }

        _pollInProgress = true;
        var hasError = false;

        try
        {
            foreach (var binding in _bindings)
            {
                try
                {
                    if (binding.Definition.Type == DeviceType.Bit)
                    {
                        var value = await _client.ReadBitAsync(binding.Prefix, binding.Address);
                        binding.UpdateBit?.Invoke(value);
                    }
                    else
                    {
                        var value = await _client.ReadWordAsync(binding.Prefix, binding.Address);
                        binding.UpdateWord?.Invoke(value);
                    }
                }
                catch (Exception ex)
                {
                    hasError = true;
                    var def = binding.Definition;
                    ThrottledStatus($"読取エラー: {def.Name} {def.Device}{def.Address} ({ex.Message})", true);
                }
            }
        }
        catch (Exception ex)
        {
            hasError = true;
            ThrottledStatus($"読取エラー: {ex.Message}", true);
        }
        finally
        {
            _pollInProgress = false;
        }

        if (hasError)
        {
            _consecutiveReadErrors++;
            if (_consecutiveReadErrors >= MaxConsecutiveReadErrors)
            {
                StopCommunication(autoStop: true);
                UpdateStatus($"読取エラーが連続したため停止しました ({_consecutiveReadErrors}回)。3秒後に再接続します。", true);
                _autoRetryTimer.Start();
            }
        }
        else
        {
            _consecutiveReadErrors = 0;
        }
    }

    private async Task WriteBitAsync(DeviceBinding binding, bool value)
    {
        if (_client == null)
        {
            UpdateStatus("未接続", true);
            return;
        }

        try
        {
            await _client.WriteBitAsync(binding.Prefix, binding.Address, value);
            Log($"書込: {binding.Definition.Name} {binding.Definition.Device}{binding.Definition.Address} = {(value ? "ON" : "OFF")}");
        }
        catch (Exception ex)
        {
            UpdateStatus($"書込エラー: {ex.Message}", true);
        }
    }

    private async Task WriteWordAsync(DeviceBinding binding, short value)
    {
        if (_client == null)
        {
            UpdateStatus("未接続", true);
            return;
        }

        try
        {
            await _client.WriteWordAsync(binding.Prefix, binding.Address, value);
            Log($"書込: {binding.Definition.Name} {binding.Definition.Device}{binding.Definition.Address} = {value}");
        }
        catch (Exception ex)
        {
            UpdateStatus($"書込エラー: {ex.Message}", true);
        }
    }

    private async Task IncrementWordAsync(DeviceBinding binding, short delta)
    {
        if (_client == null)
        {
            UpdateStatus("未接続", true);
            return;
        }

        try
        {
            var current = await _client.ReadWordAsync(binding.Prefix, binding.Address);
            var next = (short)(current + delta);
            await _client.WriteWordAsync(binding.Prefix, binding.Address, next);
            Log($"書込: {binding.Definition.Name} {binding.Definition.Device}{binding.Definition.Address} = {next}");
        }
        catch (Exception ex)
        {
            UpdateStatus($"書込エラー: {ex.Message}", true);
        }
    }

    private void OpenConnectionSettings()
    {
        using var dialog = new ConnectionSettingsForm(_connectionSettings);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _connectionSettings = dialog.Settings;
            SaveConnectionSettingsSafe();
            _ = ConnectAsync();
            ApplyAlwaysOnTop();
        }
    }

    private void OpenDeviceSettings()
    {
        var path = _deviceCsvPath ?? DeviceCsvLoader.DefaultPath();
        using var dialog = new DeviceEditorForm(path);
        dialog.ShowDialog(this);
        _deviceCsvPath = path;
        LoadDeviceCsv(path);
    }

    private void SetRadix(RadixMode mode)
    {
        _radixMode = mode;
        radixDecItem.Checked = mode == RadixMode.Dec;
        radixHexItem.Checked = mode == RadixMode.Hex;
        RenderSelectedGroup();
    }

    private void SetPollInterval(int ms)
    {
        _pollIntervalMs = ms;
        interval100Item.Checked = ms == 100;
        interval200Item.Checked = ms == 200;
        interval500Item.Checked = ms == 500;
        interval1000Item.Checked = ms == 1000;
        _pollTimer.Interval = _pollIntervalMs;
    }

    private void ToggleAlwaysOnTop()
    {
        _connectionSettings.AlwaysOnTop = alwaysOnTopItem.Checked;
        SaveConnectionSettingsSafe();
        ApplyAlwaysOnTop();
    }

    private void ApplyAlwaysOnTop()
    {
        TopMost = _connectionSettings.AlwaysOnTop;
        alwaysOnTopItem.Checked = _connectionSettings.AlwaysOnTop;
        UpdateStatus(_connectionSettings.AlwaysOnTop ? "最前面表示: ON" : "最前面表示: OFF");
    }

    private void SaveConnectionSettingsSafe()
    {
        try
        {
            _connectionSettings.Save(ConnectionSettings.DefaultPath());
        }
        catch (IOException ex)
        {
            UpdateStatus($"設定保存エラー: {ex.Message}", true);
        }
        catch (UnauthorizedAccessException ex)
        {
            UpdateStatus($"設定保存エラー: {ex.Message}", true);
        }
    }

    private void EnsureDefaultFiles()
    {
        var baseDir = AppContext.BaseDirectory;
        var baseDevices = Path.Combine(baseDir, "devices.csv");
        var baseConn = Path.Combine(baseDir, "connection.csv");

        if (!File.Exists(baseDevices))
        {
            File.WriteAllText(baseDevices, "group,name,type,mode,device,address,const,comment\n");
        }

        if (!File.Exists(baseConn))
        {
            File.WriteAllText(baseConn, "ip,192.168.3.39\nport,5001\nusePassword,false\npassword,\nisAscii,false\nisUdp,True\nrequestFrame,E4\ntimeoutMs,1000\nalwaysOnTop,True\n");
        }
        else
        {
            EnsureConnectionDefaults(baseConn);
        }
    }

    private static void EnsureConnectionDefaults(string path)
    {
        var defaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ip"] = "ip,192.168.3.39",
            ["port"] = "port,5001",
            ["usepassword"] = "usePassword,false",
            ["password"] = "password,",
            ["isascii"] = "isAscii,false",
            ["isudp"] = "isUdp,True",
            ["requestframe"] = "requestFrame,E4",
            ["timeoutms"] = "timeoutMs,1000",
            ["alwaysontop"] = "alwaysOnTop,True",
        };

        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
            {
                continue;
            }

            var parts = line.Split(',', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
            {
                continue;
            }

            existing.Add(parts[0].Trim());
        }

        var missing = new List<string>();
        foreach (var kv in defaults)
        {
            if (!existing.Contains(kv.Key))
            {
                missing.Add(kv.Value);
            }
        }

        if (missing.Count > 0)
        {
            File.AppendAllLines(path, missing);
        }
    }

    private string FormatWordValue(short value)
    {
        return _radixMode == RadixMode.Dec ? value.ToString() : $"0x{value:X}";
    }

    private bool TryParseWordInput(string text, out short value, out string error)
    {
        error = string.Empty;
        text = text.Trim();

        if (_radixMode == RadixMode.Hex)
        {
            var sanitized = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? text[2..] : text;
            if (short.TryParse(sanitized, System.Globalization.NumberStyles.HexNumber, null, out value))
            {
                return true;
            }

            error = "16進入力が不正です";
            return false;
        }

        if (short.TryParse(text, out value))
        {
            return true;
        }

        error = "10進入力が不正です";
        return false;
    }

    private void UpdateStatus(string message, bool isError = false)
    {
        statusLabel.Text = message;
        statusLabel.ForeColor = isError ? Color.DarkRed : SystemColors.ControlText;
        Log(message);
    }

    private void ThrottledStatus(string message, bool isError = false)
    {
        var now = DateTime.Now;
        if ((now - _lastErrorTime).TotalMilliseconds < 500)
        {
            return;
        }

        _lastErrorTime = now;
        UpdateStatus(message, isError);
    }

    private void AttachToolTip(Control control, string text)
    {
        control.MouseEnter += (_, __) =>
        {
            var pos = control.PointToClient(Cursor.Position);
            pos.Offset(40, 16);
            _toolTip.Show(text, control, pos, 3000);
        };

        control.MouseLeave += (_, __) => _toolTip.Hide(control);
    }

    private static string GetModeBadge(DeviceMode mode)
    {
        return mode switch
        {
            DeviceMode.Momentary => "Momentary",
            DeviceMode.Alternate => "Alternate",
            DeviceMode.Lamp => "LAMP",
            DeviceMode.Value => "Value",
            DeviceMode.SetConst => "SetConst",
            DeviceMode.Inc => "INC",
            DeviceMode.Dec => "DEC",
            DeviceMode.Display => "Display",
            _ => mode.ToString().ToUpperInvariant(),
        };
    }

    private static Color GetModeColor(DeviceMode mode)
    {
        return mode switch
        {
            DeviceMode.Momentary => Color.SteelBlue,
            DeviceMode.Alternate => Color.Teal,
            DeviceMode.Lamp => Color.SeaGreen,
            DeviceMode.Value => Color.SlateGray,
            DeviceMode.SetConst => Color.IndianRed,
            DeviceMode.Inc => Color.DarkOliveGreen,
            DeviceMode.Dec => Color.DarkOliveGreen,
            DeviceMode.Display => Color.DimGray,
            _ => Color.Gray,
        };
    }

    private static int CenterX(int areaX, int areaWidth, int controlWidth)
    {
        return areaX + (areaWidth - controlWidth) / 2;
    }

    private static void ApplyPressFeedback(Button button)
    {
        var baseBack = button.BackColor;
        var baseFore = button.ForeColor;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = Color.DimGray;
        button.FlatAppearance.MouseOverBackColor = Color.Gainsboro;

        void Reset()
        {
            button.BackColor = baseBack;
            button.ForeColor = baseFore;
        }

        button.MouseDown += (_, __) =>
        {
            button.BackColor = Color.SteelBlue;
            button.ForeColor = Color.White;
        };
        button.MouseUp += (_, __) => Reset();
        button.MouseLeave += (_, __) => Reset();
    }

    private void ToggleLogWindow()
    {
        if (logWindowItem.Checked)
        {
            if (_logWindow == null || _logWindow.IsDisposed)
            {
                _logWindow = new LogWindow();
                _logWindow.FormClosed += (_, __) =>
                {
                    logWindowItem.Checked = false;
                    _logWindow = null;
                };
            }

            _logWindow.Show(this);
            _logWindow.BringToFront();
        }
        else
        {
            _logWindow?.Close();
        }
    }

    private void ShowLogWindowOnStartup()
    {
        logWindowItem.Checked = true;
        ToggleLogWindow();
    }

    private void Log(string message)
    {
        _logWindow?.Append(message);
    }

    private void ShowAbout()
    {
        using var dialog = new AboutForm();
        dialog.ShowDialog(this);
    }
}
