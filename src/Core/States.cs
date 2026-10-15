namespace HideBar.Core
{
    enum HidingStatus
    {
        Paused,
        NothingPicked,
        NotConnected,   // monitors are picked but none of them is connected
        Hiding
    }

    enum TaskbarState
    {
        MainDisplay,    // the main display, whose taskbar HideBar leaves alone
        Visible,
        Hidden,         // hidden by HideBar
        Paused,         // picked, but HideBar is paused
        None            // no taskbar here, e.g. Windows shows it on the main display only
    }
}
