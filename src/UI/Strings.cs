using HideBar.Core;

namespace HideBar.UI
{
    // The words the window and the tray icon use for the controller's state.
    static class Strings
    {
        public static string Status(TaskbarController controller)
        {
            switch (controller.Status)
            {
                case HidingStatus.Paused:
                    return "Paused. Every taskbar is visible.";
                case HidingStatus.NothingPicked:
                    return "Pick a monitor to hide its taskbar.";
                case HidingStatus.NotConnected:
                    return "The picked monitor isn't connected.";
                default:
                    int count = controller.HiddenDisplayCount;
                    return count == 1 ? "Hiding the taskbar on 1 monitor." : "Hiding the taskbar on " + count + " monitors.";
            }
        }

        public static string StateLabel(TaskbarState state)
        {
            switch (state)
            {
                case TaskbarState.MainDisplay: return "Main display";
                case TaskbarState.Hidden: return "Taskbar hidden";
                case TaskbarState.Paused: return "Paused";
                case TaskbarState.None: return "No taskbar";
                default: return "Taskbar visible";
            }
        }

        public static string DisplayName(Display display)
        {
            return string.IsNullOrEmpty(display.Name) ? "Unnamed monitor" : display.Name;
        }

        public static string Details(Display display, TaskbarState state)
        {
            string details = display.Bounds.Width + " × " + display.Bounds.Height;
            if (display.ModelId != null) details += "  ·  " + display.ModelId;
            return details + "  ·  " + StateLabel(state);
        }
    }
}
