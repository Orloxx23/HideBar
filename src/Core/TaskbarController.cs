using System;
using System.Collections.Generic;

namespace HideBar.Core
{
    // Hides the taskbar on the picked displays, keeps it hidden, and puts everything back on
    // pause or exit. It owns the settings and saves them whenever they change.
    class TaskbarController
    {
        readonly IDisplaySystem system;
        readonly ISettingsStore store;
        readonly IStartupRegistration startup;
        readonly AppSettings settings;
        readonly HashSet<IntPtr> hiddenTaskbars = new HashSet<IntPtr>();
        int hiddenDisplayCount;

        public event EventHandler StateChanged;

        public TaskbarController(IDisplaySystem system, ISettingsStore store, IStartupRegistration startup)
        {
            this.system = system;
            this.store = store;
            this.startup = startup;
            settings = store.Load();
        }

        public bool IsPaused { get; private set; }
        public bool ReclaimSpace { get { return settings.ReclaimSpace; } }
        public bool ShowTrayIcon { get { return settings.ShowTrayIcon; } }
        public bool StartWithWindows { get { return startup.IsEnabled; } }
        public bool HasPickedDisplays { get { return settings.MonitorIds.Count > 0; } }
        public int HiddenDisplayCount { get { return hiddenDisplayCount; } }

        public HidingStatus Status
        {
            get
            {
                if (IsPaused) return HidingStatus.Paused;
                if (settings.MonitorIds.Count == 0) return HidingStatus.NothingPicked;
                if (hiddenDisplayCount == 0) return HidingStatus.NotConnected;
                return HidingStatus.Hiding;
            }
        }

        // The connected displays, numbered from left to right, with the state of their taskbars.
        public List<Display> GetDisplays()
        {
            List<Display> displays = system.GetDisplays();
            displays.Sort(CompareByPosition);

            List<Taskbar> taskbars = system.GetTaskbars();
            for (int i = 0; i < displays.Count; i++)
            {
                displays[i].Number = i + 1;
                displays[i].TaskbarVisible = HasVisibleTaskbar(displays[i], taskbars);
            }
            return displays;
        }

        public bool IsPicked(Display display)
        {
            return display.CanHideTaskbar && settings.MonitorIds.Contains(display.ModelId);
        }

        public TaskbarState GetTaskbarState(Display display)
        {
            if (display.IsPrimary) return TaskbarState.MainDisplay;
            if (IsPicked(display))
            {
                if (IsPaused) return TaskbarState.Paused;
                return display.TaskbarVisible ? TaskbarState.Visible : TaskbarState.Hidden;
            }
            return display.TaskbarVisible ? TaskbarState.Visible : TaskbarState.None;
        }

        public void SetPicked(Display display, bool picked)
        {
            if (!display.CanHideTaskbar || IsPicked(display) == picked) return;
            ChangeSettings(delegate
            {
                if (picked) settings.MonitorIds.Add(display.ModelId);
                else settings.MonitorIds.Remove(display.ModelId);
            });
        }

        public void SetReclaimSpace(bool value)
        {
            if (settings.ReclaimSpace == value) return;
            ChangeSettings(delegate { settings.ReclaimSpace = value; });
        }

        public void SetShowTrayIcon(bool value)
        {
            if (settings.ShowTrayIcon == value) return;
            settings.ShowTrayIcon = value;
            store.Save(settings);
            OnStateChanged();
        }

        public void SetStartWithWindows(bool value)
        {
            if (value) startup.Enable();
            else startup.Disable();
            OnStateChanged();
        }

        public void SetPaused(bool value)
        {
            if (IsPaused == value) return;
            if (value) Restore();
            IsPaused = value;
            Apply();
        }

        // Hides the taskbar on every picked display. It runs every second and on display changes,
        // because Explorer shows the taskbar again when it restarts or the layout changes.
        public void Apply()
        {
            hiddenDisplayCount = 0;
            if (!IsPaused && settings.MonitorIds.Count > 0)
            {
                List<Taskbar> taskbars = system.GetTaskbars();
                ForgetClosedTaskbars(taskbars);

                foreach (Display display in system.GetDisplays())
                {
                    if (!IsPicked(display)) continue;

                    HideTaskbarsOn(display, taskbars);

                    // Windows keeps reserving the hidden taskbar's space, so maximized windows would
                    // leave a gap. The space is given back without broadcasting the change, because
                    // the broadcast makes Explorer show the taskbar again.
                    if (settings.ReclaimSpace && display.WorkArea != display.Bounds)
                        system.SetWorkArea(display.Bounds, false);

                    hiddenDisplayCount++;
                }
            }
            OnStateChanged();
        }

        // Shows the taskbars HideBar hid and gives their space back.
        public void Restore()
        {
            List<Taskbar> taskbars = system.GetTaskbars();
            foreach (Taskbar taskbar in taskbars)
            {
                if (hiddenTaskbars.Contains(taskbar.Handle) && !taskbar.IsVisible) system.Show(taskbar);
            }
            hiddenTaskbars.Clear();

            bool autoHide = system.IsTaskbarAutoHide();
            foreach (Display display in system.GetDisplays())
            {
                if (!IsPicked(display)) continue;

                // An auto-hidden taskbar doesn't reserve any space.
                Rect workArea = display.Bounds;
                if (!autoHide)
                {
                    foreach (Taskbar taskbar in TaskbarsOn(display, taskbars)) workArea = workArea.Without(taskbar.Bounds);
                }
                if (workArea != display.WorkArea) system.SetWorkArea(workArea, true);
            }
        }

        // Settings that decide what is hidden are changed with everything restored first, so the
        // next Apply starts from a clean state.
        void ChangeSettings(Action change)
        {
            Restore();
            change();
            store.Save(settings);
            Apply();
        }

        void HideTaskbarsOn(Display display, List<Taskbar> taskbars)
        {
            foreach (Taskbar taskbar in TaskbarsOn(display, taskbars))
            {
                if (taskbar.IsVisible) system.Hide(taskbar);
                hiddenTaskbars.Add(taskbar.Handle);
            }
        }

        // Explorer creates new taskbar windows when it restarts; the old handles are gone.
        void ForgetClosedTaskbars(List<Taskbar> taskbars)
        {
            var current = new HashSet<IntPtr>();
            foreach (Taskbar taskbar in taskbars) current.Add(taskbar.Handle);
            hiddenTaskbars.RemoveWhere(handle => !current.Contains(handle));
        }

        void OnStateChanged()
        {
            EventHandler handler = StateChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        static IEnumerable<Taskbar> TaskbarsOn(Display display, List<Taskbar> taskbars)
        {
            foreach (Taskbar taskbar in taskbars)
            {
                if (!taskbar.IsPrimary && taskbar.Monitor == display.Handle) yield return taskbar;
            }
        }

        static bool HasVisibleTaskbar(Display display, List<Taskbar> taskbars)
        {
            foreach (Taskbar taskbar in taskbars)
            {
                if (taskbar.IsVisible && taskbar.Monitor == display.Handle) return true;
            }
            return false;
        }

        static int CompareByPosition(Display a, Display b)
        {
            int byLeft = a.Bounds.Left.CompareTo(b.Bounds.Left);
            return byLeft != 0 ? byLeft : a.Bounds.Top.CompareTo(b.Bounds.Top);
        }
    }
}
