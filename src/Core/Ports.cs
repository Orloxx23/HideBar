using System.Collections.Generic;

namespace HideBar.Core
{
    // What TaskbarController needs from the outside world. Platform has the Windows versions;
    // the tests use fakes.

    interface IDisplaySystem
    {
        List<Display> GetDisplays();
        List<Taskbar> GetTaskbars();
        void Hide(Taskbar taskbar);
        void Show(Taskbar taskbar);
        bool IsTaskbarAutoHide();

        // Sets the work area of the display that contains the rectangle. With broadcast, every
        // window is told about the change, which also makes Explorer lay out its taskbars again.
        void SetWorkArea(Rect area, bool broadcast);
    }

    interface ISettingsStore
    {
        AppSettings Load();
        void Save(AppSettings settings);
    }

    interface IStartupRegistration
    {
        bool IsEnabled { get; }
        void Enable();
        void Disable();
    }
}
