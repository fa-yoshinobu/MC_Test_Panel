using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Windows.Forms;

namespace McTestPanel;

public sealed class LogWindow : Form
{
    private const int MaxLogItems = 5000;
    private readonly ListBox _list;
    private readonly ConcurrentQueue<string> _pending = new();

    public LogWindow()
    {
        Text = "通信ログ";
        Width = 640;
        Height = 420;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Font;
        TopMost = true;
        Shown += (_, __) => EnsureVisible();

        _list = new ListBox
        {
            Dock = DockStyle.Fill,
            HorizontalScrollbar = true,
            ScrollAlwaysVisible = true,
            SelectionMode = SelectionMode.MultiExtended,
        };
        _list.KeyDown += (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                CopySelectedOrAll();
                e.SuppressKeyPress = true;
            }
        };

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8, 6, 8, 6),
        };

        var copyButton = new Button { Text = "コピー", Size = new Size(90, 30) };
        copyButton.Click += (_, __) => CopySelectedOrAll();
        panel.Controls.Add(copyButton);

        var clearButton = new Button { Text = "クリア", Size = new Size(90, 30) };
        clearButton.Click += (_, __) => _list.Items.Clear();
        panel.Controls.Add(clearButton);

        Controls.Add(_list);
        Controls.Add(panel);

        FlushPending();
    }

    private void EnsureVisible()
    {
        var area = Screen.PrimaryScreen?.WorkingArea ?? Screen.FromControl(this).WorkingArea;
        var x = Math.Max(area.Left, Math.Min(Left, area.Right - Width));
        var y = Math.Max(area.Top, Math.Min(Top, area.Bottom - Height));
        Location = new Point(x, y);
    }

    public void Append(string message)
    {
        var line = $"{DateTime.Now:HH:mm:ss} {message}";
        _pending.Enqueue(line);
        if (InvokeRequired)
        {
            BeginInvoke(FlushPending);
        }
        else
        {
            FlushPending();
        }
    }

    private void FlushPending()
    {
        if (IsDisposed) return;
        while (_pending.TryDequeue(out var line))
        {
            _list.Items.Add(line);
            while (_list.Items.Count > MaxLogItems)
            {
                _list.Items.RemoveAt(0);
            }
        }

        if (_list.Items.Count > 0)
        {
            _list.TopIndex = _list.Items.Count - 1;
        }
    }

    private void CopySelectedOrAll()
    {
        if (_list.Items.Count == 0) return;

        var lines = new System.Collections.Generic.List<string>();
        if (_list.SelectedItems.Count > 0)
        {
            foreach (var item in _list.SelectedItems)
            {
                lines.Add(item?.ToString() ?? string.Empty);
            }
        }
        else
        {
            foreach (var item in _list.Items)
            {
                lines.Add(item?.ToString() ?? string.Empty);
            }
        }

        Clipboard.SetText(string.Join(Environment.NewLine, lines));
    }
}
