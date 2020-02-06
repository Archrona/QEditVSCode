using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SpeechConsole
{
    static class Server
    {
        public static HttpListener listener;
        public static Window mainWindow;
        public static bool finalText;

        public class SimpleResult
        {
            public bool status;

            public SimpleResult(bool status) {
                this.status = status;
            }
        }

        public class TextResult
        {
            public bool status;
            public bool final;
            public string text;
            public string code;

            public TextResult(bool status, bool final, string text, string code) {
                this.status = status;
                this.final = final;
                this.text = text;
                this.code = code;
            }
        }


        public static TextResult serveText(bool final) {
            string rawText = (string)mainWindow.Invoke((Func<string>)delegate () { return mainWindow.getText(); });
            string code = Language.process(rawText, final, false);
            return new TextResult(
                true,
                finalText,
                rawText,
                code
            );
        }

        public static SimpleResult serveClear(string extension) {
            finalText = false;
            mainWindow.Invoke((Action)delegate () {
                Language.loadSubstitutions(extension);
                mainWindow.setLanguageInfo(Language.languageInfoString);
                mainWindow.clearText();
            });
            return new SimpleResult(
                true
            );
        }

        public static bool serveOneRequest() {
            HttpListenerContext context = listener.GetContext();
            HttpListenerRequest request = context.Request;

            List<string[]> query = new List<string[]>();
            if (request.Url.Query.Length > 1)
                query = request.Url.Query.Substring(1).Split('&').Select(x => x.Split('=')).ToList<string[]>();

            string path = request.Url.AbsolutePath.Substring(1);

            HttpListenerResponse response = context.Response;

            object resp = "unknown route";

            if (path == "text") {
                resp = serveText(finalText);
            }
            else if (path == "textFinal") {
                resp = serveText(true);
            }
            else if (path == "clear") {
                string ext = "";
                foreach (string[] pair in query) {
                    if (pair[0] == "ext") {
                        ext = pair[1];
                    }
                }
                resp = serveClear(ext);
            }

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(resp)
            );

            response.ContentLength64 = buffer.Length;
            response.ContentType = "application/json";
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();

            return true;
        }

        // This example requires the System and System.Net namespaces.
        public static void CreateServer() {
            finalText = false;

            listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:3141/");
            listener.Start();

            Console.WriteLine("Listening...");

            Task.Factory.StartNew(() => {
                while (serveOneRequest()) {
                    // continue
                }
            }).ContinueWith((result) => {
                listener.Stop();
            });

        }
    }
}
