using System;
using System.Collections.Generic;
using HideBar.Core;

namespace HideBar.Tests
{
    // An in-memory Windows: displays with a 48 px taskbar along the bottom, like Windows 11.
    class FakeDisplaySystem : IDisplaySystem
    {
        const int TaskbarHeight = 48;

        public readonly List<Display> Displays = new List<Display>();
        public readonly List<Taskbar> Taskbars = new List<Taskbar>();
        public readonly List<string> Calls = new List<string>();
        public bool AutoHide;

        public Display AddDisplay(int id, string modelId, Rect bounds, bool isPrimary)
        {
            var taskbarBounds = new Rect(bounds.Left, bounds.Bottom - TaskbarHeight, bounds.Right, bounds.Bottom);
            var display = new Display
            {
                Handle = new IntPtr(id),
                DeviceName = @"\\.\DISPLAY" + id,
                ModelId = modelId,
                Name = "Monitor " + modelId,
                IsPrimary = isPrimary,
                Bounds = bounds,
                WorkArea = bounds.Without(taskbarBounds)
            };
            Displays.Add(display);
            Taskbars.Add(new Taskbar
            {
                Handle = new IntPtr(1000 + id),
                Monitor = display.Handle,
                Bounds = taskbarBounds,
                IsVisible = true,
                IsPrimary = isPrimary
            });
            return display;
        }

        public Taskbar TaskbarOf(Display display)
        {
            return Taskbars.Find(taskbar => taskbar.Monitor == display.Handle);
        }

        // Like Windows, every call returns fresh objects rather than the live state.
        public List<Display> GetDisplays()
        {
            return Displays.ConvertAll(display => new Display
            {
                Handle = display.Handle,
                DeviceName = display.DeviceName,
                ModelId = display.ModelId,
                Name = display.Name,
                IsPrimary = display.IsPrimary,
                Bounds = display.Bounds,
                WorkArea = display.WorkArea
            });
        }

        public List<Taskbar> GetTaskbars()
        {
            return Taskbars.ConvertAll(taskbar => new Taskbar
            {
                Handle = taskbar.Handle,
                Monitor = taskbar.Monitor,
                Bounds = taskbar.Bounds,
                IsVisible = taskbar.IsVisible,
                IsPrimary = taskbar.IsPrimary
            });
        }

        public void Hide(Taskbar taskbar)
        {
            Live(taskbar).IsVisible = false;
            Calls.Add("hide " + taskbar.Handle);
        }

        public void Show(Taskbar taskbar)
        {
            Live(taskbar).IsVisible = true;
            Calls.Add("show " + taskbar.Handle);
        }

        public bool IsTaskbarAutoHide()
        {
            return AutoHide;
        }

        public void SetWorkArea(Rect area, bool broadcast)
        {
            Display display = Displays.Find(d => d.Bounds.Contains(area));
            if (display != null) display.WorkArea = area;
            Calls.Add("workarea " + area + (broadcast ? " broadcast" : ""));
        }

        Taskbar Live(Taskbar taskbar)
        {
            return Taskbars.Find(t => t.Handle == taskbar.Handle);
        }
    }

    class MemorySettingsStore : ISettingsStore
    {
        public AppSettings Stored = new AppSettings();
        public int SaveCount;

        public AppSettings Load()
        {
            return Stored;
        }

        public void Save(AppSettings settings)
        {
            Stored = settings;
            SaveCount++;
        }
    }

    class FakeStartup : IStartupRegistration
    {
        public bool IsEnabled { get; private set; }

        public void Enable()
        {
            IsEnabled = true;
        }

        public void Disable()
        {
            IsEnabled = false;
        }
    }
}
