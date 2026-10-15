using System;
using System.Threading;

namespace HideBar.Platform
{
    // Lets another HideBar process reach the running one through named events:
    // "HideBar.exe --exit" closes it, and opening HideBar.exe again brings up its window.
    sealed class InstanceSignals : IDisposable
    {
        const string ExitEventName = "HideBar_Exit";
        const string ShowEventName = "HideBar_Show";

        readonly EventWaitHandle exitEvent;
        readonly EventWaitHandle showEvent;
        readonly RegisteredWaitHandle exitWait;
        readonly RegisteredWaitHandle showWait;

        // The callbacks run on a thread-pool thread.
        public InstanceSignals(Action onExit, Action onShow)
        {
            exitEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ExitEventName);
            showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
            exitWait = ThreadPool.RegisterWaitForSingleObject(exitEvent, delegate { onExit(); }, null, -1, true);
            showWait = ThreadPool.RegisterWaitForSingleObject(showEvent, delegate { onShow(); }, null, -1, false);
        }

        public static void SendExit()
        {
            Signal(ExitEventName);
        }

        public static void SendShow()
        {
            // The running instance may bring its window to the front, which Windows only allows
            // when the process the user just started hands over that right.
            NativeMethods.AllowSetForegroundWindow(NativeMethods.ASFW_ANY);
            Signal(ShowEventName);
        }

        public void Dispose()
        {
            exitWait.Unregister(null);
            showWait.Unregister(null);
            exitEvent.Dispose();
            showEvent.Dispose();
        }

        static void Signal(string eventName)
        {
            EventWaitHandle handle;
            if (!EventWaitHandle.TryOpenExisting(eventName, out handle)) return;
            handle.Set();
            handle.Dispose();
        }
    }
}
