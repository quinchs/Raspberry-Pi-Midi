using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using MidiBackup.Http.RestService;
using System.Diagnostics;

namespace MidiBackup.Http
{
    public class HttpServer
    {
        private HttpListener _listener;
        private HttpRestHandler _handler;

        internal MidiDriver Driver;
        
        public HttpServer(int port, MidiDriver driver)
        {
            Logger.Write("Creating HTTP Server...", Severity.Http, Severity.Log);
            _listener = new HttpListener();

            this.Driver = driver;
#if DEBUG
            _listener.Prefixes.Add($"http://localhost:{port}/");
#else
           _listener.Prefixes.Add($"http://*:{port}/");
#endif

            _handler = new(this);


            _listener.Start();

            _ = Task.Run(async () => await HandleRequest().ConfigureAwait(false));
            Logger.Write($"Http server <Green>Online</Green>! listening on port {port}", Severity.Http, Severity.Log);
        }


        private async Task HandleRequest()
        {
            while (_listener.IsListening)
            {
                var context = await _listener.GetContextAsync().ConfigureAwait(false);
                _ = Task.Run(async () => await HandleContext(context).ConfigureAwait(false));
            }
        }

        private async Task HandleContext(HttpListenerContext context)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var code = await _handler.ProcessRestRequestAsync(context);
                sw.Stop();
                Logger.Write($"{sw.ElapsedMilliseconds}ms: {GetColorFromMethod(context.Request.HttpMethod)} => {context.Request.RawUrl} {code}");
            }
            catch (Exception x)
            {
                Logger.Write($"Uncaught exception in hanler: {x}", Severity.Http, Severity.Critical);
            }
        }

        private string GetColorFromMethod(string method)
        {
            switch (method)
            {
                case "GET":
                    return Logger.BuildColoredString(method, ConsoleColor.Green);
                case "POST":
                    return Logger.BuildColoredString(method, ConsoleColor.DarkYellow);
                case "PUT":
                    return Logger.BuildColoredString(method, ConsoleColor.Blue);
                case "DELETE":
                    return Logger.BuildColoredString(method, ConsoleColor.Red);
                default:
                    return Logger.BuildColoredString(method, ConsoleColor.Gray);

            }
        }
    }
}
