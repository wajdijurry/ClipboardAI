using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Windows.Media.Imaging;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace ClipboardAI.UI
{
    /// <summary>
    /// Event arguments for clipboard change events
    /// </summary>
    public class ClipboardChangedEventArgs : EventArgs
    {
        public bool ContainsText { get; }
        public bool ContainsImage { get; }
        public bool ContainsFileDrop { get; }
        public string Text { get; }
        public string SourceApplication { get; }
        public System.Collections.Specialized.StringCollection FileDropList { get; }
        public System.Windows.Media.Imaging.BitmapSource Image { get; }
        public byte[] ImageData { get; }

        public ClipboardChangedEventArgs(bool containsText, bool containsImage, bool containsFileDrop, 
            string text, System.Collections.Specialized.StringCollection fileDropList, string sourceApplication,
            System.Windows.Media.Imaging.BitmapSource image = null, byte[] imageData = null)
        {
            ContainsText = containsText;
            ContainsImage = containsImage;
            ContainsFileDrop = containsFileDrop;
            Text = text;
            FileDropList = fileDropList ?? new System.Collections.Specialized.StringCollection();
            SourceApplication = sourceApplication ?? "Unknown";
            Image = image;
            ImageData = imageData;
        }
    }

    /// <summary>
    /// Monitors clipboard changes using Windows API hooks
    /// </summary>
    public sealed class ClipboardMonitor : IDisposable
    {
        // Singleton instance
        private static readonly Lazy<ClipboardMonitor> _instance = 
            new Lazy<ClipboardMonitor>(() => new ClipboardMonitor());

        public static ClipboardMonitor Instance => _instance.Value;

        // Win32 API constants
        private const int WM_CLIPBOARDUPDATE = 0x031D;
        private IntPtr _windowHandle;
        private HwndSource _hwndSource;
        private bool _isMonitoring;

        // Event for clipboard changes
        public event EventHandler<ClipboardChangedEventArgs> ClipboardChanged;

        private ClipboardMonitor()
        {
            // Private constructor for singleton
        }

        public void Start()
        {
            if (_isMonitoring) return;

            // Create a dummy window for receiving messages
            _windowHandle = CreateMessageWindow();
            _isMonitoring = true;
        }

        public void Stop()
        {
            if (!_isMonitoring) return;

            // Remove the clipboard listener
            if (_windowHandle != IntPtr.Zero)
            {
                NativeMethods.RemoveClipboardFormatListener(_windowHandle);
                _hwndSource?.Dispose();
                _windowHandle = IntPtr.Zero;
            }

            _isMonitoring = false;
        }

        private IntPtr CreateMessageWindow()
        {
            // Create a message-only window to receive clipboard notifications
            _hwndSource = new HwndSource(0, 0, 0, 0, 0, "ClipboardMonitor", IntPtr.Zero);
            _hwndSource.AddHook(WndProc);

            // Register for clipboard update messages
            if (_hwndSource.Handle != IntPtr.Zero)
            {
                NativeMethods.AddClipboardFormatListener(_hwndSource.Handle);
            }

            return _hwndSource.Handle;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_CLIPBOARDUPDATE)
            {
                // Process clipboard update
                ProcessClipboardChange();
                handled = true;
            }

            return IntPtr.Zero;
        }

        private void ProcessClipboardChange()
        {
            try
            {
                // Add a small delay to prevent multiple events firing for the same clipboard operation
                var now = DateTime.Now;
                if ((now - _lastProcessTime).TotalMilliseconds < 100)
                {
                    // Skip if we just processed an event (within 100ms)
                    return;
                }
                _lastProcessTime = now;

                bool containsText = Clipboard.ContainsText();
                bool containsImage = Clipboard.ContainsImage();
                bool containsFileDrop = Clipboard.ContainsFileDropList();
                string text = containsText ? Clipboard.GetText() : string.Empty;
                System.Collections.Specialized.StringCollection fileDropList = containsFileDrop ? Clipboard.GetFileDropList() : new System.Collections.Specialized.StringCollection();
                string sourceApp = GetClipboardOwnerApp();
                System.Windows.Media.Imaging.BitmapSource image = containsImage ? Clipboard.GetImage() : null;
                byte[] imageData = containsImage ? GetImageData(image) : null;

                // Create a content hash to detect duplicates
                string contentHash = CreateContentHash(containsText, text, containsImage, imageData, containsFileDrop, fileDropList);

                // Skip if this is a duplicate of the last clipboard content
                if (contentHash == _lastContentHash)
                {
                    return;
                }

                // Update the last content hash
                _lastContentHash = contentHash;

                // Raise the event
                ClipboardChanged?.Invoke(this, new ClipboardChangedEventArgs(containsText, containsImage, containsFileDrop, text, fileDropList, sourceApp, image, imageData));
            }
            catch (Exception)
            {
                // Clipboard operations can sometimes fail if another application has the clipboard locked
                // Just ignore these exceptions
            }
        }

        // Create a hash of the clipboard content to detect duplicates
        private string CreateContentHash(bool containsText, string text, bool containsImage, byte[] imageData, bool containsFileDrop, StringCollection fileDropList)
        {
            using (var sha = SHA256.Create())
            {
                StringBuilder sb = new StringBuilder();

                // Add content type flags
                sb.Append(containsText ? "T" : "");
                sb.Append(containsImage ? "I" : "");
                sb.Append(containsFileDrop ? "F" : "");

                // Add text content hash if present
                if (containsText && !string.IsNullOrEmpty(text))
                {
                    byte[] textBytes = Encoding.UTF8.GetBytes(text);
                    byte[] textHash = sha.ComputeHash(textBytes);
                    sb.Append(Convert.ToBase64String(textHash));
                }

                // Add image content hash if present
                if (containsImage && imageData != null && imageData.Length > 0)
                {
                    // Use a smaller portion of the image data for hashing to improve performance
                    byte[] sampleData = SampleImageData(imageData, 1024); // Sample up to 1KB
                    byte[] imageHash = sha.ComputeHash(sampleData);
                    sb.Append(Convert.ToBase64String(imageHash));
                }

                // Add file drop list hash if present
                if (containsFileDrop && fileDropList != null && fileDropList.Count > 0)
                {
                    string files = string.Join("|", fileDropList.Cast<object>().Select(x => x.ToString()));
                    byte[] fileBytes = Encoding.UTF8.GetBytes(files);
                    byte[] fileHash = sha.ComputeHash(fileBytes);
                    sb.Append(Convert.ToBase64String(fileHash));
                }

                return sb.ToString();
            }
        }

        // Sample a portion of the image data for hashing
        private byte[] SampleImageData(byte[] data, int maxBytes)
        {
            if (data == null || data.Length == 0)
                return new byte[0];

            if (data.Length <= maxBytes)
                return data;

            // Sample evenly spaced bytes from the image data
            byte[] sample = new byte[maxBytes];
            double step = (double)data.Length / maxBytes;

            for (int i = 0; i < maxBytes; i++)
            {
                int index = (int)(i * step);
                if (index < data.Length)
                    sample[i] = data[index];
            }

            return sample;
        }

        private string _lastContentHash = null;
        private DateTime _lastProcessTime = DateTime.MinValue;

        private string GetClipboardOwnerApp()
        {
            try
            {
                // Get the foreground window handle
                IntPtr hwnd = NativeMethods.GetForegroundWindow();
                if (hwnd == IntPtr.Zero)
                    return "Unknown";

                // Get the process ID for the window
                uint processId;
                NativeMethods.GetWindowThreadProcessId(hwnd, out processId);

                // Get the process name
                try
                {
                    Process process = Process.GetProcessById((int)processId);
                    return process.ProcessName;
                }
                catch
                {
                    return "Unknown";
                }
            }
            catch
            {
                return "Unknown";
            }
        }

        private byte[] GetImageData(System.Windows.Media.Imaging.BitmapSource image)
        {
            if (image == null)
                return null;

            using (var stream = new System.IO.MemoryStream())
            {
                var encoder = new System.Windows.Media.Imaging.BmpBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
                encoder.Save(stream);
                return stream.ToArray();
            }
        }

        public void Dispose()
        {
            Stop();
        }

        // Native methods for clipboard monitoring
        private static class NativeMethods
        {
            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool AddClipboardFormatListener(IntPtr hwnd);

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

            [DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll", SetLastError = true)]
            public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        }
    }
}
