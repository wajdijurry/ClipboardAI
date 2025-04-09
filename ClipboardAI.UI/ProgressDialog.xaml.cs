using System;
using System.Windows;

namespace ClipboardAI.UI
{
    /// <summary>
    /// Interaction logic for ProgressDialog.xaml
    /// </summary>
    public partial class ProgressDialog : Window
    {
        public ProgressDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets or sets the message displayed in the dialog
        /// </summary>
        public string Message
        {
            get { return MessageTextBlock.Text; }
            set { MessageTextBlock.Text = value; }
        }

        /// <summary>
        /// Gets or sets the progress value (0-100)
        /// </summary>
        public int Progress
        {
            get { return (int)ProgressBar.Value; }
            set
            {
                // Ensure value is within range
                int validValue = Math.Max(0, Math.Min(100, value));
                
                // Update UI on UI thread
                Dispatcher.Invoke(() =>
                {
                    ProgressBar.Value = validValue;
                    ProgressTextBlock.Text = $"{validValue}%";
                    
                    // Close dialog if progress is 100% without using DialogResult
                    if (validValue >= 100)
                    {
                        try
                        {
                            // Just close the window instead of setting DialogResult
                            Close();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error closing progress dialog: {ex.Message}");
                        }
                    }
                });
            }
        }
        
        /// <summary>
        /// Creates a new progress dialog with the specified title and message
        /// </summary>
        public ProgressDialog(string title, string message)
        {
            InitializeComponent();
            Title = title;
            Message = message;
        }
        
        /// <summary>
        /// Updates the progress value
        /// </summary>
        public void UpdateProgress(int value)
        {
            Progress = value;
        }
    }
}
