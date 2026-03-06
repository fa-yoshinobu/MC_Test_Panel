using System.ComponentModel;
using System.Windows.Forms;

namespace McTestPanel;

partial class MainForm
{
    private IContainer components = null;
    private MenuStrip menuStrip;
    private ToolStripMenuItem fileMenu;
    private ToolStripMenuItem openDeviceCsvItem;
    private ToolStripMenuItem reloadDeviceCsvItem;
    private ToolStripMenuItem exitItem;
    private ToolStripMenuItem settingsMenu;
    private ToolStripMenuItem connectionSettingsItem;
    private ToolStripMenuItem deviceSettingsItem;
    private ToolStripMenuItem helpMenu;
    private ToolStripMenuItem aboutItem;
    private ToolStripMenuItem viewMenu;
    private ToolStripMenuItem radixMenu;
    private ToolStripMenuItem radixDecItem;
    private ToolStripMenuItem radixHexItem;
    private ToolStripMenuItem intervalMenu;
    private ToolStripMenuItem interval100Item;
    private ToolStripMenuItem interval200Item;
    private ToolStripMenuItem interval500Item;
    private ToolStripMenuItem interval1000Item;
    private ToolStripMenuItem alwaysOnTopItem;
    private ToolStripMenuItem commMenu;
    private ToolStripMenuItem commStartItem;
    private ToolStripMenuItem commStopItem;
    private ToolStripMenuItem logWindowItem;
    private SplitContainer splitContainer;
    private ListBox groupList;
    private FlowLayoutPanel devicePanel;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel connectionStatusLabel;
    private ToolStripStatusLabel fileStatusLabel;
    private ToolStripStatusLabel statusLabel;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        menuStrip = new MenuStrip();
        fileMenu = new ToolStripMenuItem();
        openDeviceCsvItem = new ToolStripMenuItem();
        reloadDeviceCsvItem = new ToolStripMenuItem();
        exitItem = new ToolStripMenuItem();
        settingsMenu = new ToolStripMenuItem();
        connectionSettingsItem = new ToolStripMenuItem();
        deviceSettingsItem = new ToolStripMenuItem();
        helpMenu = new ToolStripMenuItem();
        aboutItem = new ToolStripMenuItem();
        viewMenu = new ToolStripMenuItem();
        radixMenu = new ToolStripMenuItem();
        radixDecItem = new ToolStripMenuItem();
        radixHexItem = new ToolStripMenuItem();
        intervalMenu = new ToolStripMenuItem();
        interval100Item = new ToolStripMenuItem();
        interval200Item = new ToolStripMenuItem();
        interval500Item = new ToolStripMenuItem();
        interval1000Item = new ToolStripMenuItem();
        alwaysOnTopItem = new ToolStripMenuItem();
        commMenu = new ToolStripMenuItem();
        commStartItem = new ToolStripMenuItem();
        commStopItem = new ToolStripMenuItem();
        logWindowItem = new ToolStripMenuItem();
        splitContainer = new SplitContainer();
        groupList = new ListBox();
        devicePanel = new FlowLayoutPanel();
        statusStrip = new StatusStrip();
        connectionStatusLabel = new ToolStripStatusLabel();
        fileStatusLabel = new ToolStripStatusLabel();
        statusLabel = new ToolStripStatusLabel();

        menuStrip.SuspendLayout();
        ((ISupportInitialize)splitContainer).BeginInit();
        splitContainer.Panel1.SuspendLayout();
        splitContainer.Panel2.SuspendLayout();
        splitContainer.SuspendLayout();
        statusStrip.SuspendLayout();
        SuspendLayout();

        menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, settingsMenu, commMenu, viewMenu, helpMenu });
        menuStrip.Location = new System.Drawing.Point(0, 0);
        menuStrip.Name = "menuStrip";
        menuStrip.Size = new System.Drawing.Size(980, 24);
        menuStrip.TabIndex = 0;

        fileMenu.Text = "ファイル";
        fileMenu.DropDownItems.AddRange(new ToolStripItem[] { openDeviceCsvItem, reloadDeviceCsvItem, exitItem });

        openDeviceCsvItem.Text = "デバイスCSVを開く";
        reloadDeviceCsvItem.Text = "再読み込み";
        exitItem.Text = "終了";

        settingsMenu.Text = "設定";
        settingsMenu.DropDownItems.AddRange(new ToolStripItem[] { connectionSettingsItem, deviceSettingsItem });
        connectionSettingsItem.Text = "接続設定";
        deviceSettingsItem.Text = "デバイス設定";

        helpMenu.Text = "ヘルプ";
        helpMenu.DropDownItems.AddRange(new ToolStripItem[] { aboutItem });
        aboutItem.Text = "バージョン";

        viewMenu.Text = "表示";
        viewMenu.DropDownItems.AddRange(new ToolStripItem[] { radixMenu, intervalMenu, alwaysOnTopItem });

        radixMenu.Text = "進数";
        radixMenu.DropDownItems.AddRange(new ToolStripItem[] { radixDecItem, radixHexItem });
        radixDecItem.Text = "10進";
        radixHexItem.Text = "16進";
        radixDecItem.Checked = true;

        intervalMenu.Text = "更新周期";
        intervalMenu.DropDownItems.AddRange(new ToolStripItem[] { interval100Item, interval200Item, interval500Item, interval1000Item });
        interval100Item.Text = "100ms";
        interval200Item.Text = "200ms";
        interval500Item.Text = "500ms";
        interval1000Item.Text = "1000ms";
        interval500Item.Checked = true;

        alwaysOnTopItem.Text = "常に最前面";
        alwaysOnTopItem.CheckOnClick = true;

        commMenu.Text = "通信";
        commMenu.DropDownItems.AddRange(new ToolStripItem[] { commStartItem, commStopItem, logWindowItem });
        commStartItem.Text = "開始";
        commStopItem.Text = "停止";
        logWindowItem.Text = "ログ表示";
        logWindowItem.CheckOnClick = true;

        splitContainer.Dock = DockStyle.Fill;
        splitContainer.Location = new System.Drawing.Point(0, 24);
        splitContainer.Name = "splitContainer";
        splitContainer.SplitterDistance = 140;

        groupList.Dock = DockStyle.Fill;
        groupList.Name = "groupList";
        groupList.ItemHeight = 15;
        splitContainer.Panel1.Controls.Add(groupList);

        devicePanel.Dock = DockStyle.Fill;
        devicePanel.AutoScroll = true;
        devicePanel.FlowDirection = FlowDirection.TopDown;
        devicePanel.WrapContents = false;
        devicePanel.Padding = new Padding(4);
        splitContainer.Panel2.Controls.Add(devicePanel);

        statusStrip.Items.AddRange(new ToolStripItem[] { connectionStatusLabel, fileStatusLabel, statusLabel });
        statusStrip.Location = new System.Drawing.Point(0, 640);
        statusStrip.Name = "statusStrip";
        statusStrip.Size = new System.Drawing.Size(980, 22);

        connectionStatusLabel.Text = "接続: 未接続";
        fileStatusLabel.Text = "CSV: -";
        statusLabel.Spring = true;
        statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(980, 662);
        Controls.Add(splitContainer);
        Controls.Add(statusStrip);
        Controls.Add(menuStrip);
        MainMenuStrip = menuStrip;
        MinimumSize = new System.Drawing.Size(840, 520);
        Name = "MainForm";
        Text = "MC Test Panel";

        menuStrip.ResumeLayout(false);
        menuStrip.PerformLayout();
        splitContainer.Panel1.ResumeLayout(false);
        splitContainer.Panel2.ResumeLayout(false);
        ((ISupportInitialize)splitContainer).EndInit();
        splitContainer.ResumeLayout(false);
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
