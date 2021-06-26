using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidiBackup.Http.RestService
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal class Route : Attribute
    {
        public string _name { get; }
        public bool _isRegex { get; }
        public string _method { get; }

        public Route(string Name, string Method)
        {
            this._name = Name;
            this._method = Method;
        }

        public Route(string Name, string Method, bool Regex)
        {
            this._name = Name;
            this._method = Method;
            this._isRegex = Regex;
        }
    }
}
