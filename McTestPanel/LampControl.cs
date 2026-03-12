using System.Drawing;
using System.Windows.Forms;

namespace McTestPanel;

public sealed class LampControl : Control
{
    private bool _isOn;

    public LampControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        Size = new Size(26, 26);
        BackColor = SystemColors.Control;
    }

    public bool IsOn
    {
        get => _isOn;
        set
        {
            if (_isOn == value) return;
            _isOn = value;
            Invalidate();
        }
    }

    public Color OnColor { get; set; } = Color.LimeGreen;
    public Color OffColor { get; set; } = Color.Gray;
    public Color OnBorder { get; set; } = Color.DarkGreen;
    public Color OffBorder { get; set; } = Color.DimGray;

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var rect = new Rectangle(2, 2, Width - 4, Height - 4);
        using var brush = new SolidBrush(_isOn ? OnColor : OffColor);
        using var pen = new Pen(_isOn ? OnBorder : OffBorder, 2);

        e.Graphics.FillEllipse(brush, rect);
        e.Graphics.DrawEllipse(pen, rect);
    }
}
