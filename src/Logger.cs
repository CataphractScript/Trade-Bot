using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Visualize
{
    internal static class Logger
    {
        // Reference to the RichTextBox control
        private static RichTextBox _outputBox;

        // Initialize with the RichTextBox instance (logTextBox)
        public static void Initialize(RichTextBox logTextBox)
        {
            _outputBox = logTextBox;
        }

        // Log a message with default color
        public static void Log(string message)
        {
            AppendText(message, _outputBox?.ForeColor ?? Color.Black);
        }

        // Log a message with specified color
        public static void Log(string message, Color color)
        {
            AppendText(message, color);
        }

        // Append text method
        private static void AppendText(string message, Color color)
        {
            if (_outputBox == null)
            {
                Console.WriteLine(message);
                return;
            }

            if (_outputBox.InvokeRequired)
            {
                _outputBox.Invoke(new Action(() => AppendText(message, color)));
            }
            else
            {
                _outputBox.SelectionStart = _outputBox.TextLength;
                _outputBox.SelectionLength = 0;
                _outputBox.SelectionColor = color;
                _outputBox.AppendText(message + Environment.NewLine);
                _outputBox.SelectionColor = _outputBox.ForeColor;
                _outputBox.ScrollToCaret();
            }
        }
    }


}
