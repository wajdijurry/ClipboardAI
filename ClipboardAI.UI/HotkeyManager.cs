using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;

namespace ClipboardAI.UI
{
    /// <summary>
    /// Manages global hotkeys for the application
    /// </summary>
    public class HotkeyManager
    {
        // Win32 API imports for hotkey registration
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // Modifier keys
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
        private const uint MOD_NOREPEAT = 0x4000;

        // Window handle for hotkey registration
        private IntPtr _windowHandle;
        
        // Dictionary to map hotkey IDs to actions
        private Dictionary<int, Action> _hotkeyActions = new Dictionary<int, Action>();
        
        // Dictionary to track registered hotkeys
        private Dictionary<string, int> _registeredHotkeys = new Dictionary<string, int>();
        
        // Counter for hotkey IDs
        private int _hotkeyId = 1;
        
        // Event for hotkey pressed
        public event EventHandler<string> HotkeyPressed;

        /// <summary>
        /// Initialize the hotkey manager for a specific window
        /// </summary>
        /// <param name="window">The window to register hotkeys for</param>
        public void Initialize(Window window)
        {
            // Get the window handle
            _windowHandle = new WindowInteropHelper(window).Handle;
            
            // Add hook for window messages
            HwndSource source = HwndSource.FromHwnd(_windowHandle);
            source.AddHook(WndProc);
        }

        /// <summary>
        /// Register a hotkey
        /// </summary>
        /// <param name="hotkeyName">Name of the hotkey</param>
        /// <param name="hotkeyString">Hotkey string (e.g. "Ctrl+Alt+V")</param>
        /// <param name="action">Action to execute when hotkey is pressed</param>
        /// <returns>True if registration was successful</returns>
        public bool RegisterHotkey(string hotkeyName, string hotkeyString, Action action)
        {
            // Unregister existing hotkey with this name if it exists
            UnregisterHotkey(hotkeyName);
            
            // Parse the hotkey string
            if (!ParseHotkeyString(hotkeyString, out uint modifiers, out uint key))
                return false;
            
            // Register the hotkey
            int id = _hotkeyId++;
            bool result = RegisterHotKey(_windowHandle, id, modifiers, key);
            
            if (result)
            {
                _hotkeyActions[id] = action;
                _registeredHotkeys[hotkeyName] = id;
            }
            
            return result;
        }

        /// <summary>
        /// Unregister a hotkey
        /// </summary>
        /// <param name="hotkeyName">Name of the hotkey to unregister</param>
        public void UnregisterHotkey(string hotkeyName)
        {
            if (_registeredHotkeys.TryGetValue(hotkeyName, out int id))
            {
                UnregisterHotKey(_windowHandle, id);
                _hotkeyActions.Remove(id);
                _registeredHotkeys.Remove(hotkeyName);
            }
        }

        /// <summary>
        /// Unregister all hotkeys
        /// </summary>
        public void UnregisterAllHotkeys()
        {
            foreach (var id in _registeredHotkeys.Values)
            {
                UnregisterHotKey(_windowHandle, id);
            }
            
            _hotkeyActions.Clear();
            _registeredHotkeys.Clear();
        }

        /// <summary>
        /// Parse a hotkey string into modifiers and key
        /// </summary>
        /// <param name="hotkeyString">Hotkey string (e.g. "Ctrl+Alt+V")</param>
        /// <param name="modifiers">Output modifiers</param>
        /// <param name="key">Output key</param>
        /// <returns>True if parsing was successful</returns>
        private bool ParseHotkeyString(string hotkeyString, out uint modifiers, out uint key)
        {
            modifiers = 0;
            key = 0;
            
            if (string.IsNullOrEmpty(hotkeyString))
                return false;
            
            string[] parts = hotkeyString.Split('+');
            
            // Last part is the key
            string keyString = parts[parts.Length - 1].Trim();
            
            // Convert key string to virtual key code
            if (!Enum.TryParse(keyString, out System.Windows.Input.Key wpfKey))
                return false;
            
            // Convert WPF key to virtual key code
            key = (uint)KeyInterop.VirtualKeyFromKey(wpfKey);
            
            // Parse modifiers
            for (int i = 0; i < parts.Length - 1; i++)
            {
                string mod = parts[i].Trim().ToLower();
                
                switch (mod)
                {
                    case "ctrl":
                        modifiers |= MOD_CONTROL;
                        break;
                    case "alt":
                        modifiers |= MOD_ALT;
                        break;
                    case "shift":
                        modifiers |= MOD_SHIFT;
                        break;
                    case "win":
                        modifiers |= MOD_WIN;
                        break;
                    default:
                        return false;
                }
            }
            
            return true;
        }

        /// <summary>
        /// Window procedure to handle hotkey messages
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                
                if (_hotkeyActions.TryGetValue(id, out Action action))
                {
                    action?.Invoke();
                    
                    // Find the hotkey name for this ID
                    foreach (var kvp in _registeredHotkeys)
                    {
                        if (kvp.Value == id)
                        {
                            HotkeyPressed?.Invoke(this, kvp.Key);
                            break;
                        }
                    }
                    
                    handled = true;
                }
            }
            
            return IntPtr.Zero;
        }
    }
}
