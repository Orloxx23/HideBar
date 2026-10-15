using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using HideBar.Core;
using HideBar.Platform;

namespace HideBar.UI
{
    // HideBar's window. Closing it leaves HideBar running in the background.
    class SettingsWindow : Form
    {
        const int ContentWidth = 600;
        const string SourceUrl = "https://github.com/Orloxx23/HideBar";

        readonly TaskbarController controller;
        readonly Theme theme = Theme.FromWindows();
        readonly ToolTip toolTip = new ToolTip();
        readonly Label titleLabel;
        readonly Label subtitleLabel;
        readonly MonitorsCard monitorsCard;
        readonly OptionsCard optionsCard;
        readonly Button identifyButton;
        readonly Button pauseButton;
        readonly Button exitButton;
        readonly Label footerLabel;
        readonly LinkLabel sourceLink;

        public SettingsWindow(TaskbarController controller, Action exitApp)
        {
            this.controller = controller;
            int width = Style.S(ContentWidth);

            Text = "HideBar";
            Icon = AppIcon.Large;
            Font = Style.Body;
            BackColor = theme.Background;
            ForeColor = theme.Text;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.None;
            AutoScroll = true;

            titleLabel = Style.MakeLabel("HideBar", Style.Title, theme.Text, theme.Background);
            subtitleLabel = Style.MakeLabel("Hide the taskbar on the monitors you choose and keep it on the rest.",
                Style.Body, theme.SubtleText, theme.Background);

            monitorsCard = new MonitorsCard(theme, controller, width);
            optionsCard = new OptionsCard(theme, controller, width);

            identifyButton = Style.MakeButton(theme, "Identify monitors");
            identifyButton.Click += delegate { IdentifyOverlay.ShowFor(controller.GetDisplays(), theme); };
            toolTip.SetToolTip(identifyButton, "Show each monitor's number on the monitor itself.");

            pauseButton = Style.MakeButton(theme, "Pause", "Resume");
            pauseButton.Click += delegate { controller.SetPaused(!controller.IsPaused); };

            exitButton = Style.MakeButton(theme, "Exit HideBar");
            exitButton.Click += delegate { exitApp(); };
            toolTip.SetToolTip(exitButton, "Close HideBar and show every taskbar again.");

            footerLabel = Style.MakeLabel("Version " + Program.Version + "  ·  Closing this window keeps HideBar running.",
                Style.Small, theme.SubtleText, theme.Background);
            sourceLink = MakeLink("Source code", SourceUrl);

            Controls.AddRange(new Control[]
            {
                titleLabel, subtitleLabel, monitorsCard, optionsCard,
                identifyButton, pauseButton, exitButton, footerLabel, sourceLink
            });

            monitorsCard.SizeChanged += delegate { LayoutContent(); };
            controller.StateChanged += OnStateChanged;
            RefreshState();
            LayoutContent();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            WindowStyling.UseDarkTitleBar(Handle, theme.IsDark);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            controller.StateChanged -= OnStateChanged;
            toolTip.Dispose();
            base.OnFormClosed(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        void OnStateChanged(object sender, EventArgs e)
        {
            if (!IsDisposed) RefreshState();
        }

        void RefreshState()
        {
            monitorsCard.RefreshState();
            optionsCard.RefreshState();
            string pauseText = controller.IsPaused ? "Resume" : "Pause";
            if (pauseButton.Text != pauseText) pauseButton.Text = pauseText;
        }

        // Stacks everything top to bottom. Runs again when the Monitors card changes height.
        void LayoutContent()
        {
            int margin = Style.S(24);
            int width = Style.S(ContentWidth);
            Point scroll = AutoScrollPosition;
            int left = margin + scroll.X;
            int top = margin + scroll.Y;

            titleLabel.Location = new Point(left - Style.S(2), top);
            top = titleLabel.Bottom;
            subtitleLabel.Location = new Point(left, top);
            top = subtitleLabel.Bottom + Style.S(16);

            monitorsCard.Location = new Point(left, top);
            top = monitorsCard.Bottom + Style.S(12);
            optionsCard.Location = new Point(left, top);
            top = optionsCard.Bottom + Style.S(16);

            identifyButton.Location = new Point(left, top);
            pauseButton.Location = new Point(identifyButton.Right + Style.S(8), top);
            exitButton.Location = new Point(left + width - exitButton.Width, top);
            top = exitButton.Bottom + Style.S(14);

            footerLabel.Location = new Point(left, top);
            sourceLink.Location = new Point(left + width - sourceLink.Width, top);
            top = footerLabel.Bottom + margin;

            int contentHeight = top - scroll.Y;
            int maxHeight = Screen.FromPoint(Cursor.Position).WorkingArea.Height - Style.S(64);
            int clientWidth = width + 2 * margin;
            if (contentHeight > maxHeight) clientWidth += SystemInformation.VerticalScrollBarWidth;
            ClientSize = new Size(clientWidth, Math.Min(contentHeight, maxHeight));
        }

        LinkLabel MakeLink(string text, string url)
        {
            var link = new LinkLabel
            {
                Text = text,
                Font = Style.Small,
                AutoSize = true,
                BackColor = theme.Background,
                LinkColor = theme.AccentText,
                ActiveLinkColor = theme.AccentText,
                VisitedLinkColor = theme.AccentText,
                LinkBehavior = LinkBehavior.HoverUnderline
            };
            link.LinkClicked += delegate { Process.Start(url); };
            link.Size = link.PreferredSize;
            return link;
        }
    }
}
