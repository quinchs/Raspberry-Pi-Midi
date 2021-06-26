using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MidiBackup.Http.RestService.Info
{
    internal class RestMethodInfo
    {
        public string RouteName
            => Route._name;
        public string RouteMethod
            => Route._method;

        private Route Route;
        private MethodInfo Info;

        private Regex RouteParamRegex { get; }
        private MatchEvaluator RouteParamEvaluator = new((a) => $"(?<{a.Groups[1].Value}>.+?)");

        private Dictionary<(int index, string name), Type> Parameters { get; } = new();

        public bool IsMatch(string route, string method)
        {
            if (this.Route._isRegex && this.RouteMethod == method)
                return Regex.IsMatch(this.Route._name, route);
            else return RouteParamRegex.IsMatch(route) && method == this.RouteMethod;
        }

        public RestMethodInfo(Route route, MethodInfo info)
        {
            this.Route = route;
            this.Info = info;

            if (!route._isRegex)
                this.RouteParamRegex = ConstructRouteParamRegex(route._name);

            var parameters = info.GetParameters();

            for(int i = 0; i != parameters.Length; i++)
            {
                var param = parameters[i];
                this.Parameters.Add((i, param.Name), param.ParameterType);
            }
        }

        public object[] GetConvertedParameters(string route)
        {
            if (this.Route._isRegex)
            {
                var regType = Parameters.FirstOrDefault();
                if (regType.Equals(default(KeyValuePair<string, Type>)))
                    return new object[] { };

                if(regType.Value == typeof(MatchCollection))
                    return new object[] { Regex.Matches(route, this.Route._name) };
                else if (regType.Value == typeof(Match))
                    return new object[] { Regex.Match(route, this.Route._name) };
                else return new object[] { };
            }

            object[] arr = new object[this.Parameters.Count];

            var routeParams = GetRouteParams(route);

            foreach(var item in routeParams)
            {
                var rawParam = this.Parameters.FirstOrDefault(x => x.Key.name == item.Key);

                if (rawParam.Value == null)
                    continue;

                try
                {
                    arr[rawParam.Key.index] = Convert.ChangeType(item.Value, rawParam.Value);
                }
                catch(Exception x)
                {
                    return null;
                }
            }

            return arr;
        }

        public Task<RestResult> Execute(object instance, params object[] parameters)
            => (Task<RestResult>)this.Info.Invoke(instance, parameters);
        public async Task<RestResult> ExecuteAsync(object instance, params object[] parameters)
            => await ((Task<RestResult>)this.Info.Invoke(instance, parameters)).ConfigureAwait(false);

        private Regex ConstructRouteParamRegex(string route)
        {
            var val = Regex.Replace(route, @"{(.+?)}", RouteParamEvaluator);
            return new Regex($"^{val}(?>/|)$".Replace("/", "\\/"));
        }

        private Dictionary<string, string> GetRouteParams(string route)
        {
            var matches = RouteParamRegex.Matches(route);

            var dict = new Dictionary<string, string>();

            foreach (var item in Parameters)
            {
                var match = matches.FirstOrDefault(x => x.Groups.ContainsKey(item.Key.name));

                if (match == null)
                    continue;

                dict.Add(item.Key.name, match.Groups[item.Key.name].Value);
            }

            return dict;
        }
    }
}
