using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup.Http.RestService.Info
{
    internal class RestModuleInfo
    {
        public List<RestMethodInfo> Routes { get; } = new();

        private Type classType { get; }

        public RestModuleInfo(Type type)
        {
            if (!type.IsClass || !type.IsAssignableTo(typeof(RestModuleBase)))
                throw new ArgumentException("Type must be child class of RestModuleBase");

            this.classType = type;

            var methods = type.GetMethods().Where(x => x.GetCustomAttribute<Route>() != null && x.ReturnType == typeof(Task<RestResult>)).Select(x => (x.GetCustomAttribute<Route>(), x));

            foreach (var method in methods)
            {
                Routes.Add(new RestMethodInfo(method.Item1, method.x));
            }
        }

        public bool HasRoute(HttpListenerRequest request)
            => Routes.Any(x =>
            {
                Logger.Debug($"{x == null} {x}", Severity.Http, Severity.Log);
                try
                {
                    return x.IsMatch(request.RawUrl, request.HttpMethod);
                }
                catch(Exception ex)
                {
                    Logger.Write($"{ex}", Severity.Http, Severity.Critical);
                    return false;
                }
            });

        public RestMethodInfo GetRoute(HttpListenerRequest request)
            => Routes.FirstOrDefault(x => x.IsMatch(request.RawUrl, request.HttpMethod));

        public RestModuleBase GetInstance()
            => (RestModuleBase)Activator.CreateInstance(classType);

        public override bool Equals(object obj)
        {
            if(obj is RestModuleInfo other)
            {
                return this.classType == other.classType;
            }
            else return base.Equals(obj);
        }

        public override string ToString()
        {
            return classType.Name;
        }
    }
}
