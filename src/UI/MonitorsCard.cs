using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using HideBar.Core;
using HideBar.UI.Controls;

namespace HideBar.UI
{
    // The Monitors card: a picture of the display layout and a row with a switch per display.
    // Its height changes with the number of displays.
    class MonitorsCard : Card
    {
        readonly TaskbarController controller;
        readonly Label heading;
        readonly Label statusLabel;
        readonly MonitorLayoutView layoutView;
        readonly Label hintLabel;
        readonly List<MonitorRow> rows = new List<MonitorRow>();
        List<Display> displays = new List<Display>();
        string layoutSignature;

        public MonitorsCard(Theme theme, TaskbarController controller, int width)
            : base(theme)
        {
            this.controller = controller;
            Width = width;

            heading = Style.MakeLabel("Monitors", Style.Heading, theme.Text, theme.Surface);
            heading.Location = new Point(Inset, Inset);
            statusLabel = Style.MakeLabel("", Style.Small, theme.SubtleText, theme.Surface);

            layoutView = new MonitorLayoutView(theme);
            layoutView.SetBounds(Inset, heading.Bottom + Style.S(8), InnerWidth, Style.S(150));
            layoutView.DisplayClicked += delegate (Display display) { controller.SetPicked(display, !controller.IsPicked(display)); };

            hintLabel = Style.MakeParagraph(
                "Click a monitor or flip its switch to hide its taskbar. The main display always keeps its taskbar.",
                Style.Small, theme.SubtleText, theme.Surface, InnerWidth);
            hintLabel.Location = new Point(Inset, layoutView.Bottom + Style.S(8));

            Controls.AddRange(new Control[] { heading, statusLabel, layoutView, hintLabel });
        }

        public void RefreshState()
        {
            displays = controller.GetDisplays();

            string signature = Signature(displays);
            if (signature != layoutSignature)
            {
                layoutSignature = signature;
                RebuildRows();
            }

            for (int i = 0; i < rows.Count; i++)
            {
                Display display = displays[i];
                string details = Strings.Details(display, controller.GetTaskbarState(display));
                rows[i].Bind(display, controller.IsPicked(display), Strings.DisplayName(display), details);
            }

            layoutView.SetDisplays(displays, controller.IsPicked, Describe);
            SetStatus(Strings.Status(controller));
        }

        // One row per display, rebuilt only when displays are added, removed or moved.
        void RebuildRows()
        {
            SuspendLayout();
            foreach (MonitorRow row in rows)
            {
                Controls.Remove(row);
                row.Dispose();
            }
            rows.Clear();

            int top = hintLabel.Bottom + Style.S(12);
            for (int i = 0; i < displays.Count; i++)
            {
                var row = new MonitorRow(theme, InnerWidth);
                row.Location = new Point(Inset, top);
                int index = i;
                row.PickedChanged += delegate { controller.SetPicked(displays[index], rows[index].Picked); };

                rows.Add(row);
                Controls.Add(row);
                top = row.Bottom;
            }
            Height = top + Style.S(6);
            ResumeLayout();
        }

        void SetStatus(string text)
        {
            if (statusLabel.Text == text) return;
            statusLabel.Text = text;
            statusLabel.Size = statusLabel.PreferredSize;
            statusLabel.Location = new Point(Width - Inset - statusLabel.Width, heading.Top + (heading.Height - statusLabel.Height) / 2);
        }

        string Describe(Display display)
        {
            return Strings.DisplayName(display) + "\n" + Strings.Details(display, controller.GetTaskbarState(display));
        }

        static string Signature(List<Display> list)
        {
            var signature = new StringBuilder();
            foreach (Display display in list)
            {
                signature.Append(display.DeviceName).Append('|').Append(display.ModelId).Append('|')
                         .Append(display.Bounds).Append('|').Append(display.IsPrimary).Append(';');
            }
            return signature.ToString();
        }
    }
}
