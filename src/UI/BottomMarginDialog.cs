using System.Drawing;
using System.Windows.Forms;
using HideBar.Core;
using HideBar.Platform;

namespace HideBar.UI
{
    sealed class BottomMarginDialog : Form
    {
        readonly Theme theme;
        readonly CheckBox useDefault;
        readonly NumericUpDown pixels;

        public BottomMarginDialog(Theme theme, int monitorNumber, int defaultMargin, int? marginOverride)
        {
            this.theme = theme;
            Text = "Bottom margin for monitor " + monitorNumber;
            Font = Style.Body;
            BackColor = theme.Surface;
            ForeColor = theme.Text;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(Style.S(340), Style.S(150));

            useDefault = new CheckBox
            {
                AutoSize = true,
                Location = new Point(Style.S(16), Style.S(16)),
                Text = "Use general setting (" + defaultMargin + " px)",
                Checked = !marginOverride.HasValue
            };
            pixels = new NumericUpDown
            {
                AccessibleName = "Bottom margin in pixels",
                Maximum = AppSettings.MaxBottomMarginPixels,
                Value = marginOverride ?? defaultMargin,
                Location = new Point(Style.S(16), Style.S(51)),
                Width = Style.S(76),
                Enabled = marginOverride.HasValue,
                BackColor = theme.Background,
                ForeColor = theme.Text
            };
            useDefault.CheckedChanged += delegate
            {
                pixels.Enabled = !useDefault.Checked;
                if (pixels.Enabled) pixels.Focus();
            };

            Label unit = Style.MakeLabel("px (0 uses the full area)", Style.Small, theme.SubtleText, theme.Surface);
            unit.Location = new Point(pixels.Right + Style.S(8), pixels.Top + Style.S(4));
            Button save = Style.MakeButton(theme, "Save");
            Button cancel = Style.MakeButton(theme, "Cancel");
            cancel.Location = new Point(ClientSize.Width - Style.S(16) - cancel.Width, Style.S(100));
            save.Location = new Point(cancel.Left - Style.S(8) - save.Width, cancel.Top);
            save.DialogResult = DialogResult.OK;
            cancel.DialogResult = DialogResult.Cancel;
            AcceptButton = save;
            CancelButton = cancel;
            Controls.AddRange(new Control[] { useDefault, pixels, unit, save, cancel });
        }

        public int? BottomMarginOverride
        {
            get { return useDefault.Checked ? (int?)null : (int)pixels.Value; }
        }

        protected override void OnHandleCreated(System.EventArgs e)
        {
            base.OnHandleCreated(e);
            WindowStyling.UseDarkTitleBar(Handle, theme.IsDark);
        }
    }
}
