using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace SpeechConsole
{
    static class Program
    {
        public static string testSpeech = "Procedure create insertion point step step let editor be get editor call finish let document be editor. Document finish let selection be editor.selection equal finish let word be document. get text of selection finish step down editor. Edit of lambda edit builder step edit builder. Replace of expand selection, step down insertion marker. Left plus word plus insertion marker. Right step step step finish step down editor. Selections is list new flat VS code. big selection of expand new flat VS code. Big position of selection. End. Line, selection. End. Character step, step down new flat VS code. Big position of selection. End. Line, selection. End. Character finish step step finish step";
        
        private static readonly HttpClient client = new HttpClient();

        public delegate bool CallBackPtr(int hwnd, int lParam);
        private static CallBackPtr callBackPtr;


        [DllImport("user32.dll")]
        internal static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumWindows(CallBackPtr lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, [Out] StringBuilder lParam);


        private static readonly uint WM_GETTEXTLENGTH = 0x0009;
        private static readonly uint WM_GETTEXT = 0x000D;

        public static string GetWindowTextRaw(IntPtr hwnd) {
            int length = (int)SendMessage(hwnd, WM_GETTEXTLENGTH, IntPtr.Zero, null);
            Console.WriteLine(length);
            StringBuilder sb = new StringBuilder(length + 1);
            SendMessage(hwnd, WM_GETTEXT, (IntPtr)sb.Capacity, sb);
            return sb.ToString();
        }

        public static bool Report(int hwnd, int lParam) {
            uint processID;
            IntPtr ptr = new IntPtr(hwnd);
            GetWindowThreadProcessId(ptr, out processID);
            string text = GetWindowTextRaw(ptr);
            Console.WriteLine("Window: " + text + " - id " + hwnd + " - process " + processID);
            return true;
        }

        public static void transfer() {

            informText(Server.mainWindow.getText(), true);

            Process[] p = Process.GetProcessesByName("Code");
            foreach (Process k in p) {
                string title = k.MainWindowTitle;
                if (title.Length > 0) {
                    Console.WriteLine(k.Id + " - " + k.ProcessName + " - " + k.MainWindowHandle + " - " + k.MainWindowTitle);
                    SetForegroundWindow(k.MainWindowHandle);
                }
            }

            //callBackPtr = new CallBackPtr(Report);
            //EnumWindows(callBackPtr, new System.IntPtr(0));

            /*if (p != null) {
                if (p.MainWindowHandle == IntPtr.Zero) {
                    ShowWindow(p.Handle, 9);
                }
                SetForegroundWindow(p.MainWindowHandle);
            }*/
        }




        private class TextMessage
        {
            public string text;
            public bool final;
        }

        public static void informText(string text, bool final = false) {
            TextMessage tm = new TextMessage();
            tm.text = Language.process(text, final, false);
            tm.final = final;

            string json = JsonConvert.SerializeObject(tm);

            Task task = Task.Run(async () => {
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage m = await client.PostAsync("http://localhost:3145/", content);
                Console.WriteLine("Status: " + m.StatusCode + ", Reason: " + m.ReasonPhrase);
            }).ContinueWith((Task t) => {
                if (t.IsFaulted) {
                    Console.WriteLine("Update could not be sent.");
                }
                else {
                    Console.WriteLine("Update sent.");
                }
            });
        }

        static void benchmark() {
            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();

            for (int i = 0; i < 100; i++) {
                Language.process(testSpeech + testSpeech + testSpeech + testSpeech, false, false);
            }

            stopwatch.Stop();
            Console.WriteLine(stopwatch.Elapsed.TotalSeconds);
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {

            client.Timeout = new TimeSpan(0, 0, 0, 0, 100); // 100 ms

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Server.mainWindow = new Window();

            Language.initialize();
            Server.mainWindow.setLanguageInfo(Language.languageInfoString);
            Server.CreateServer();
            Application.Run(Server.mainWindow);

        }

        
    }
}
