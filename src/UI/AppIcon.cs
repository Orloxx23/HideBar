using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace HideBar.UI
{
    // The HideBar icon, embedded in the exe by build.cmd from assets\HideBar.ico.
    static class AppIcon
    {
        const string ResourceName = "HideBar.ico";

        public static Icon Small { get { return Load(SystemInformation.SmallIconSize); } }
        public static Icon Large { get { return Load(SystemInformation.IconSize); } }

        static Icon Load(Size size)
        {
            using (Stream stream = typeof(AppIcon).Assembly.GetManifestResourceStream(ResourceName))
            {
                return stream != null ? new Icon(stream, size) : new Icon(SystemIcons.Application, size);
            }
        }
    }
}
