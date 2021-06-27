using MidiBackup.Http.RestService.Info;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup.Http.RestService
{
    internal class HttpRestHandler
    {
        private LinkedList<RestModuleBase> CachedModules { get; } = new();
        private int cacheSize = 15;
        private List<RestModuleInfo> Modules { get; } = new();

        private HttpServer Server { get; }

        public HttpRestHandler(HttpServer server)
        {
            Logger.Write("Creating Rest handler...", Severity.Http, Severity.Log);
            this.Server = server;
            LoadRoutes();
            Logger.Write($"Rest handler {Logger.BuildColoredString("Online", ConsoleColor.Green)}! Loaded {Modules.Count} Modules with {Modules.Select(x => x.Routes.Count).Sum()} routes!", Severity.Http, Severity.Log);
        }

        private void LoadRoutes()
        {
            var modules = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsAssignableTo(typeof(RestModuleBase)) && x != typeof(RestModuleBase));

            foreach(var module in modules)
            {
                this.Modules.Add(new RestModuleInfo(module));
            }
        }

        public bool TryGetModule(HttpListenerRequest request, out RestModuleBase Module, out RestModuleInfo Info)
        {
            Module = null;
            Info = null;

            lock (CachedModules)
            {
                if((Module = CachedModules.FirstOrDefault(x => x.ModuleInfo.HasRoute(request))) != null)
                {
                    Info = Module.ModuleInfo;
                    BumpOrEnqueueModule(Module);
                    return true;
                }
            }

            var modInfo = Modules.FirstOrDefault(x => x.HasRoute(request));

            if (modInfo == null)
                return false;

            Module = modInfo.GetInstance();
            Info = modInfo;
            BumpOrEnqueueModule(Module);
            return true;
        }

        private void BumpOrEnqueueModule(RestModuleBase baseModule)
        {
            if (CachedModules.ToArray().Any(x => x.Equals(baseModule)))
            {
                CachedModules.Remove(baseModule);
                
            }
            CachedModules.AddFirst(baseModule);

            if (CachedModules.Count > cacheSize)
                CachedModules.RemoveLast();
        }

        public async Task<int> ProcessRestRequestAsync(HttpListenerContext context)
        {
            context.Response.Headers.Add("Access-Control-Allow-Headers", "*");
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            context.Response.Headers.Add("ccess-Control-Allow-Methods", "*");

            if (!TryGetModule(context.Request, out var module, out var info))
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
                return 404;
            }

            if (module == null)
            {
                context.Response.StatusCode = 404;
                context.Response.Close();
                return 404;
            }

            var moduleBase = module.InitializeModule(context, info, Server);

            var route = info.GetRoute(context.Request);

            var parms = route.GetConvertedParameters(context.Request.RawUrl);

            if(parms == null)
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
                return 400;
            }

            var task = route.ExecuteAsync(moduleBase, parms);
            var result = await task;

            if (task.Exception != null)
            {
                Logger.Write($"Uncaught exception in route {route.RouteName}!\n{task.Exception}", Severity.Http, Severity.Error);
                context.Response.StatusCode = 500;
                context.Response.Close();
                return 500;
            }
            else
            {
                if (result.Code == int.MaxValue)
                    return -1;

                if (result.Data != null)
                {
                    context.Response.ContentType = "application/json";
                    context.Response.ContentEncoding = Encoding.UTF8;

                    string json = JsonConvert.SerializeObject(result.Data);

                    context.Response.OutputStream.Write(Encoding.UTF8.GetBytes(json));
                }

                context.Response.StatusCode = result.Code;
                context.Response.Close();
                return result.Code;
            }
        }
    }
}
