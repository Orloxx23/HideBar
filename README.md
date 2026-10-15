# HideBar

Hide the Windows taskbar on the monitors you choose and keep it on the rest.

Windows only lets you show the taskbar on every monitor or on the main one. HideBar covers the case in between: pick a secondary monitor from its tray menu and that monitor's taskbar goes away, while your other monitors keep theirs. It started as a fix for a small 7" screen mounted inside a PC case, where a taskbar only gets in the way.

## Features

- Pick one or more secondary monitors from the tray menu.
- Maximized windows get the full height of the screen, because the space the taskbar reserved is given back.
- The taskbar stays hidden when Explorer restarts, a monitor is reconnected or the display layout changes.
- Remembers your choice and can start with Windows.
- Pausing or exiting brings everything back to normal.
- A single small `.exe` with no installer and no dependencies beyond what Windows already ships.

## Requirements

Windows 10 (version 1607 or later) or Windows 11. HideBar runs on .NET Framework 4.x, which is part of Windows.

## Build

No Visual Studio needed. `build.cmd` uses the C# compiler that comes with .NET Framework:

```bat
git clone https://github.com/Orloxx23/hide-windonws-taskbar.git
cd hide-windonws-taskbar
build.cmd
```

This creates `HideBar.exe` next to the source.

## Usage

1. Run `HideBar.exe`. Its icon appears in the notification area; if you don't see it, look under the `^` arrow.
2. Right-click the icon, open **Hide the taskbar on** and check the monitor.

| Menu item | What it does |
|---|---|
| Hide the taskbar on | Lists your monitors with their resolution and model ID. Checked monitors have no taskbar. |
| Pause | Brings the taskbars back until you uncheck it. Double-clicking the icon does the same. |
| Start with Windows | Runs HideBar when you sign in. |
| Exit and show the taskbar again | Closes HideBar and restores everything. |

`HideBar.exe --exit` closes the running instance, which is handy in scripts and shortcuts.

Your choice is saved in `%APPDATA%\HideBar\monitors.txt`, one model ID per line.

## How it works

Each secondary taskbar in Windows 10 and 11 is an Explorer window of class `Shell_SecondaryTrayWnd`. HideBar finds the one on each chosen monitor, hides it with `ShowWindow`, and sets that monitor's work area to its full bounds with `SystemParametersInfo(SPI_SETWORKAREA)`.

The work area is changed without broadcasting `WM_SETTINGCHANGE`. That broadcast makes Explorer lay out its taskbars again, which shows the hidden one. A timer checks every second, and again on every display change, and hides the taskbar if Explorer brought it back.

Monitors are matched by the model ID in their EDID (for example `DWE2100`), so the choice survives plugging the monitor into another port or reordering your displays.

## Limitations

- Only secondary taskbars can be hidden, not the one on the main display.
- Windows that were already maximized on the monitor keep their old size until you maximize them again. This follows from not broadcasting the work area change.
- Two monitors of the same model share a model ID, so checking one checks both.

## License

[MIT](LICENSE)
