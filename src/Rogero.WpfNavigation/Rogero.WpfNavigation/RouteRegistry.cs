using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Rogero.Options;

namespace Rogero.WpfNavigation
{
    public class RouteRegistry
    {
        public Guid Id { get; } = Guid.NewGuid();

        private readonly IDictionary<string, ViewVmPair> _uriMap = new ConcurrentDictionary<string, ViewVmPair>();

        public void Register<T>(string uri, Func<object> viewModelFactory)
        {
            Register(uri, viewModelFactory, typeof(T));
        }

        public void Register(string uri, Func<object> viewModelFactory, Type view)
        {
            _uriMap.Add(uri, new ViewVmPair(view, viewModelFactory));
        }

        public Option<ViewVmPair> FindViewVm(string uri, object initData)
        {
            return _uriMap.TryGetValue(uri);
        }
    }
}
