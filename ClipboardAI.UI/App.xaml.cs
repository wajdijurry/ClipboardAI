using System;
using System.Windows;
using ClipboardAI.Common;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Linq;

namespace ClipboardAI.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex _mutex = null;
        private const string MutexName = "Global\\ClipboardAIApplicationMutex";

        protected override void OnStartup(StartupEventArgs e)
        {
            // Check for existing instance
            bool createdNew;
            
            try
            {
                _mutex = new Mutex(false, MutexName, out createdNew);
                
                if (!createdNew)
                {
                    // Another instance is already running
                    ActivateExistingInstance();
                    Shutdown();
                    return;
                }
                
                // Acquire the mutex - this ensures only one instance runs
                if (!_mutex.WaitOne(0, false))
                {
                    // Could not acquire the mutex, another instance is running
                    ActivateExistingInstance();
                    Shutdown();
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking for existing instance: {ex.Message}", "ClipboardAI", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            base.OnStartup(e);
            
            // Initialize plugin manager to load plugins from the plugins directory
            string pluginsDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
            ClipboardAI.Plugins.PluginManager.Instance.Initialize(pluginsDirectory);
            
            // Initialize feature manager to load enabled features
            var featureManager = FeatureManager.Instance;
            
            // Initialize clipboard monitoring
            ClipboardMonitor.Instance.Start();
            
            // Handle application exit
            Application.Current.Exit += (s, args) =>
            {
                ClipboardMonitor.Instance.Stop();
                
                // Release the mutex when the application exits
                if (_mutex != null)
                {
                    try
                    {
                        _mutex.ReleaseMutex();
                        _mutex.Dispose();
                    }
                    catch (Exception)
                    {
                        // Ignore exceptions during cleanup
                    }
                }
            };
        }

        private void ActivateExistingInstance()
        {
            try
            {
                // Find the window of the existing instance and bring it to the foreground
                Process current = Process.GetCurrentProcess();
                var processes = Process.GetProcessesByName(current.ProcessName)
                    .Where(p => p.Id != current.Id && p.MainWindowHandle != IntPtr.Zero)
                    .ToList();
                
                if (processes.Any())
                {
                    var existingProcess = processes.First();
                    NativeMethods.ShowWindow(existingProcess.MainWindowHandle, NativeMethods.SW_RESTORE);
                    NativeMethods.SetForegroundWindow(existingProcess.MainWindowHandle);
                }
                else
                {
                    MessageBox.Show("Another instance of ClipboardAI is already running, check system trays.", 
                        "ClipboardAI", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error activating existing instance: {ex.Message}", 
                    "ClipboardAI", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Native methods for window manipulation
    internal static class NativeMethods
    {
        public const int SW_RESTORE = 9;
        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}
