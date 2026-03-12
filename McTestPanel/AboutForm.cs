using System.Drawing;
using System.Windows.Forms;

namespace McTestPanel;

public sealed class AboutForm : Form
{
    public AboutForm()
    {
        Text = "バージョン";
        Width = 600;
        Height = 440;
        MinimumSize = new Size(520, 360);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        AutoScaleMode = AutoScaleMode.Font;
        TopMost = true;
        Shown += (_, __) => EnsureVisible();

        var title = new Label
        {
            Text = "MC Test Panel",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 40,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 8, 12, 0),
        };

        var version = new Label
        {
            Text = $"Version: {VersionInfo.GetAppVersion()}",
            Dock = DockStyle.Top,
            Height = 24,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 12, 0),
        };

        var box = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            Dock = DockStyle.Fill,
            ScrollBars = ScrollBars.Vertical,
            Text = VersionInfo.GetLicenseText() + "\r\n" + VersionInfo.GetAppLicenseBody(),
        };

        var link = new LinkLabel
        {
            Text = VersionInfo.ProjectUrl,
            Dock = DockStyle.Top,
            AutoSize = true,
            MaximumSize = new Size(560, 0),
            Padding = new Padding(12, 0, 12, 0),
        };
        link.LinkClicked += (_, __) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = VersionInfo.ProjectUrl,
            UseShellExecute = true,
        });

        var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Size = new Size(90, 30) };
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(12, 6, 12, 6),
        };
        panel.Controls.Add(okButton);

        Controls.Add(box);
        Controls.Add(panel);
        Controls.Add(version);
        Controls.Add(link);
        Controls.Add(title);

        AcceptButton = okButton;
    }

    private void EnsureVisible()
    {
        var area = Screen.PrimaryScreen?.WorkingArea ?? Screen.FromControl(this).WorkingArea;
        var x = Math.Max(area.Left, Math.Min(Left, area.Right - Width));
        var y = Math.Max(area.Top, Math.Min(Top, area.Bottom - Height));
        Location = new Point(x, y);
    }
}
