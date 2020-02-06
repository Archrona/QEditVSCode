using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace SpeechConsole
{
    public partial class Window : Form
    {
        [DllImport("user32.dll")]
        internal static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private bool textChanged;
        private int ticksSinceChanged;

        public string getText() {
            return input.Text;
        }

        public void clearText() {
            input.Text = "";

            Process currentProcess = Process.GetCurrentProcess();
            IntPtr hWnd = currentProcess.MainWindowHandle;
            if (hWnd != IntPtr.Zero) {
                SetForegroundWindow(hWnd);
                //ShowWindow(hWnd, User32.SW_MAXIMIZE);
            }

            input.Focus();
        }

        public void setLanguageInfo(string info) {
            this.Text = "QEdit SC: " + info;
        }

        public Window() {
            InitializeComponent();

            textChanged = false;
            ticksSinceChanged = 0;
        }

        private void input_TextChanged(object sender, EventArgs e) {
            textChanged = true;
            ticksSinceChanged = 0;
        }

        private void Window_Load(object sender, EventArgs e) {

        }

        private void Window_KeyDown(object sender, KeyEventArgs e) {
            if (e.Control && e.Alt && e.KeyCode == Keys.P) {
                Program.transfer();
            }
        }

        private void textChangeMonitor_Tick(object sender, EventArgs e) {
            if (textChanged) {
                ticksSinceChanged++;
                if (ticksSinceChanged >= 3) {
                    Program.informText(input.Text);
                    textChanged = false;
                    ticksSinceChanged = 0;
                }
            } else {
                ticksSinceChanged = 0;
            }
            
        }
    }
}
