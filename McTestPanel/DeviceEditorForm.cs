using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace McTestPanel;

public sealed class DeviceEditorForm : Form
{
    private readonly BindingList<DeviceDefinitionRow> _rows = new();
    private readonly DataGridView _grid;
    private readonly ToolStripStatusLabel _status;
    private string _csvPath;
    private readonly DataGridViewComboBoxColumn _typeColumn;
    private readonly DataGridViewComboBoxColumn _modeColumn;
    private readonly DataGridViewComboBoxColumn _deviceColumn;

    public DeviceEditorForm(string csvPath)
    {
        _csvPath = csvPath;

        Text = "デバイス設定";
        Width = 980;
        Height = 640;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Font;
        TopMost = true;
        Shown += (_, __) => EnsureVisible();

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 60,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(8, 8, 8, 24),
        };

        var addButton = new Button { Text = "追加", Size = new Size(90, 36) };
        var deleteButton = new Button { Text = "削除", Size = new Size(90, 36) };
        var reloadButton = new Button { Text = "再読み込み", Size = new Size(100, 36) };
        var saveButton = new Button { Text = "保存", Size = new Size(90, 36) };

        addButton.Click += (_, __) => AddRow();
        deleteButton.Click += (_, __) => DeleteSelected();
        reloadButton.Click += (_, __) => LoadFromCsv(_csvPath);
        saveButton.Click += (_, __) => SaveToCsv(_csvPath);

        panel.Controls.Add(addButton);
        panel.Controls.Add(deleteButton);
        panel.Controls.Add(reloadButton);
        panel.Controls.Add(saveButton);

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            AllowUserToAddRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true,
            DataSource = _rows,
            ScrollBars = ScrollBars.Both,
        };

        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DeviceDefinitionRow.Group), HeaderText = "グループ", Width = 120, Frozen = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DeviceDefinitionRow.Name), HeaderText = "名称", Width = 200, Frozen = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });

        _typeColumn = new DataGridViewComboBoxColumn
        {
            DataPropertyName = nameof(DeviceDefinitionRow.Type),
            HeaderText = "種別",
            Width = 90,
            DataSource = new[] { "Bit", "Word" },
            Frozen = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
        };
        _grid.Columns.Add(_typeColumn);

        _modeColumn = new DataGridViewComboBoxColumn
        {
            DataPropertyName = nameof(DeviceDefinitionRow.Mode),
            HeaderText = "モード",
            Width = 140,
            Frozen = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
        };
        _modeColumn.Items.AddRange(new object[] { "Momentary", "Alternate", "Lamp", "Value", "SetConst", "Inc", "Dec", "Display" });
        _grid.Columns.Add(_modeColumn);

        _deviceColumn = new DataGridViewComboBoxColumn
        {
            DataPropertyName = nameof(DeviceDefinitionRow.Device),
            HeaderText = "デバイス",
            Width = 90,
            Frozen = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
        };
        _deviceColumn.Items.AddRange(new object[]
        {
            "B", "CC", "CN", "CS", "D", "DX", "DY", "F", "L", "M", "R", "S",
            "SB", "SC", "SD", "SM", "SN", "SS", "SW",
            "TC", "TN", "TS", "V", "W", "X", "Y", "Z", "ZR",
        });
        _grid.Columns.Add(_deviceColumn);
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DeviceDefinitionRow.Address), HeaderText = "アドレス", Width = 120, Frozen = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DeviceDefinitionRow.Const), HeaderText = "設定値", Width = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(DeviceDefinitionRow.Comment), HeaderText = "コメント", Width = 260, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

        _grid.CellValueChanged += GridOnCellValueChanged;
        _grid.CurrentCellDirtyStateChanged += GridOnCurrentCellDirtyStateChanged;
        _grid.EditingControlShowing += GridOnEditingControlShowing;
        _grid.DataError += GridOnDataError;

        var statusStrip = new StatusStrip();
        _status = new ToolStripStatusLabel { Spring = true, TextAlign = System.Drawing.ContentAlignment.MiddleLeft };
        statusStrip.Items.Add(_status);

        Controls.Add(_grid);
        Controls.Add(panel);
        Controls.Add(statusStrip);

        LoadFromCsv(_csvPath);
    }

    private void EnsureVisible()
    {
        var area = Screen.PrimaryScreen?.WorkingArea ?? Screen.FromControl(this).WorkingArea;
        var x = Math.Max(area.Left, Math.Min(Left, area.Right - Width));
        var y = Math.Max(area.Top, Math.Min(Top, area.Bottom - Height));
        Location = new System.Drawing.Point(x, y);
    }

    public IReadOnlyList<DeviceDefinitionRow> Rows => _rows;

    private static readonly string[] BitModes = { "Momentary", "Alternate", "Lamp" };
    private static readonly string[] WordModes = { "Value", "SetConst", "Inc", "Dec", "Display" };

    private void AddRow()
    {
        var index = _grid.CurrentCell?.RowIndex ?? _rows.Count;
        index = Math.Clamp(index, 0, _rows.Count);

        _rows.Insert(index, new DeviceDefinitionRow());
        _grid.ClearSelection();
        _grid.Rows[index].Selected = true;
        _grid.CurrentCell = _grid.Rows[index].Cells[0];
    }

    private void GridOnCurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        if (_grid.IsCurrentCellDirty)
        {
            _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

    private void GridOnCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

        var column = _grid.Columns[e.ColumnIndex];
        if (column == _typeColumn)
        {
            var row = _grid.Rows[e.RowIndex].DataBoundItem as DeviceDefinitionRow;
            if (row == null) return;

            if (row.Type.Equals("Bit", StringComparison.OrdinalIgnoreCase))
            {
                row.Mode = BitModes.Contains(row.Mode, StringComparer.OrdinalIgnoreCase) ? row.Mode : "Momentary";
            }
            else if (row.Type.Equals("Word", StringComparison.OrdinalIgnoreCase))
            {
                row.Mode = WordModes.Contains(row.Mode, StringComparer.OrdinalIgnoreCase) ? row.Mode : "Value";
            }

            _grid.Refresh();
        }
    }

    private void GridOnEditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
    {
        if (_grid.CurrentCell?.OwningColumn != _modeColumn) return;

        if (e.Control is not ComboBox combo) return;

        combo.DataSource = null;
        combo.Items.Clear();
        var row = _grid.CurrentRow?.DataBoundItem as DeviceDefinitionRow;
        var modes = row != null && row.Type.Equals("Word", StringComparison.OrdinalIgnoreCase) ? WordModes : BitModes;
        combo.Items.AddRange(modes);
        if (row != null && !combo.Items.Contains(row.Mode))
        {
            combo.Items.Add(row.Mode);
        }
    }

    private void GridOnDataError(object? sender, DataGridViewDataErrorEventArgs e)
    {
        e.ThrowException = false;
        SetStatus("無効な選択肢がありました。Type/Modeを確認してください。");
    }

    private static string ToTitle(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        var lower = value.Trim().ToLowerInvariant();
        return lower switch
        {
            "bit" => "Bit",
            "word" => "Word",
            "momentary" => "Momentary",
            "alternate" => "Alternate",
            "lamp" => "Lamp",
            "value" => "Value",
            "setconst" => "SetConst",
            "inc" => "Inc",
            "dec" => "Dec",
            "display" => "Display",
            _ => char.ToUpperInvariant(lower[0]) + lower[1..],
        };
    }

    private void DeleteSelected()
    {
        var selected = _grid.SelectedRows.Cast<DataGridViewRow>().ToList();
        if (selected.Count == 0) return;

        foreach (var row in selected)
        {
            if (row.DataBoundItem is DeviceDefinitionRow data)
            {
                _rows.Remove(data);
            }
        }
    }

    private void LoadFromCsv(string path)
    {
        _rows.Clear();

        if (!File.Exists(path))
        {
            SetStatus("CSVがありません。新規作成してください。");
            return;
        }

        var result = DeviceCsvLoader.Load(path);
        foreach (var def in result.Definitions)
        {
            _rows.Add(new DeviceDefinitionRow
            {
                Group = def.Group,
                Name = def.Name,
                Type = ToTitle(def.Type.ToString()),
                Mode = ToTitle(def.Mode.ToString()),
                Device = def.Device,
                Address = def.Address,
                Const = def.Const == 0 ? string.Empty : def.Const.ToString(),
                Comment = def.Comment,
            });
        }

        if (result.Errors.Count > 0)
        {
            SetStatus("CSV読み込み警告: " + string.Join(" / ", result.Errors));
        }
        else
        {
            SetStatus("CSV読み込み完了");
        }
    }

    private void SaveToCsv(string path)
    {
        if (!ValidateRows(out var error))
        {
            SetStatus("エラー: " + error);
            return;
        }

        try
        {
            DeviceCsvWriter.Save(path, _rows);
            SetStatus("保存しました: " + Path.GetFileName(path));
        }
        catch (IOException ex)
        {
            SetStatus("保存失敗: " + ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            SetStatus("保存失敗: " + ex.Message);
        }
    }

    private bool ValidateRows(out string error)
    {
        error = string.Empty;

        foreach (var row in _rows)
        {
            if (string.IsNullOrWhiteSpace(row.Group) || string.IsNullOrWhiteSpace(row.Name))
            {
                error = "group/name は必須です";
                return false;
            }

            if (!Enum.TryParse<DeviceType>(row.Type, true, out _))
            {
                error = "type 不正: " + row.Name;
                return false;
            }

            if (!Enum.TryParse<DeviceMode>(row.Mode, true, out _))
            {
                error = "mode 不正: " + row.Name;
                return false;
            }

            if (string.IsNullOrWhiteSpace(row.Device) || string.IsNullOrWhiteSpace(row.Address))
            {
                error = "device/address は必須: " + row.Name;
                return false;
            }

            if (!string.IsNullOrWhiteSpace(row.Const) && !short.TryParse(row.Const.Trim().Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out _))
            {
                if (!short.TryParse(row.Const.Trim(), out _))
                {
                    error = "const 不正: " + row.Name;
                    return false;
                }
            }
        }

        return true;
    }

    private void SetStatus(string message)
    {
        _status.Text = message;
    }
}
