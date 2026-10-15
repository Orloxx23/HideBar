using System.Drawing;
using HideBar.Core;
using HideBar.UI.Controls;

namespace HideBar.UI
{
    // The Options card.
    class OptionsCard : Card
    {
        readonly TaskbarController controller;
        readonly OptionRow startupRow;
        readonly OptionRow reclaimRow;
        readonly OptionRow trayIconRow;
        int nextTop;

        public OptionsCard(Theme theme, TaskbarController controller, int width)
            : base(theme)
        {
            this.controller = controller;
            Width = width;

            var heading = Style.MakeLabel("Options", Style.Heading, theme.Text, theme.Surface);
            heading.Location = new Point(Inset, Inset);
            Controls.Add(heading);
            nextTop = heading.Bottom + Style.S(10);

            startupRow = AddRow("Start with Windows",
                "Run HideBar in the background when you sign in.");
            reclaimRow = AddRow("Use the freed space",
                "Maximized windows stretch down to where the hidden taskbar was.");
            trayIconRow = AddRow("Show the icon in the notification area",
                "When it's off, open HideBar.exe again to come back to this window.");

            startupRow.CheckedChanged += delegate { controller.SetStartWithWindows(startupRow.Checked); };
            reclaimRow.CheckedChanged += delegate { controller.SetReclaimSpace(reclaimRow.Checked); };
            trayIconRow.CheckedChanged += delegate { controller.SetShowTrayIcon(trayIconRow.Checked); };

            Height = nextTop + Style.S(6);
        }

        public void RefreshState()
        {
            startupRow.Checked = controller.StartWithWindows;
            reclaimRow.Checked = controller.ReclaimSpace;
            trayIconRow.Checked = controller.ShowTrayIcon;
        }

        OptionRow AddRow(string title, string description)
        {
            var row = new OptionRow(theme, InnerWidth, title, description);
            row.Location = new Point(Inset, nextTop);
            Controls.Add(row);
            nextTop = row.Bottom;
            return row;
        }
    }
}
