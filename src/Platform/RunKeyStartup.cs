using Microsoft.Win32;
using HideBar.Core;

namespace HideBar.Platform
{
    // "Start with Windows", as an entry in the current user's Run key.
    class RunKeyStartup : IStartupRegistration
    {
        const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string ValueName = "HideBar";

        readonly string command;

        public RunKeyStartup(string executablePath, string argument)
        {
            command = "\"" + executablePath + "\" " + argument;
        }

        public bool IsEnabled
        {
            get
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath))
                {
                    return key != null && key.GetValue(ValueName) != null;
                }
            }
        }

        public void Enable()
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath))
            {
                key.SetValue(ValueName, command);
            }
        }

        public void Disable()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
            {
                if (key != null) key.DeleteValue(ValueName, false);
            }
        }

        // Points an existing entry at this copy of HideBar, in case it was moved or updated.
        public void Refresh()
        {
            if (IsEnabled) Enable();
        }
    }
}
