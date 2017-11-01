using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Windows;
using Rogero.Options;

namespace Rogero.WpfNavigation
{
    public interface IRouteEntry
    {
        string Name { get; }
        string Uri { get; }
        Type ViewModelType { get; }
        Type ViewType { get; }

        UIElement CreateView();
        object CreateViewModel();
    }

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

        public Option<ViewVmPair> GetViewVmPair(string uri, object initData)
        {
            return _uriMap.TryGetValue(uri);
        }
    }

    public interface IRouteEntryRegistry
    {
        Guid Id { get; }
        void RegisterRouteEntry(IRouteEntry routeEntry);
        Option<IRouteEntry> GetRouteEntry(string uri);
    }

    public class RouteEntryRegistry : IRouteEntryRegistry
    {
        public Guid Id { get; } = Guid.NewGuid();

        private readonly IDictionary<string, IRouteEntry> _routeEntries = new ConcurrentDictionary<string, IRouteEntry>();

        public RouteEntryRegistry()
        {
            
        }

        public Option<IRouteEntry> GetRouteEntry(string uri) => _routeEntries.TryGetValue(uri);
        public void RegisterRouteEntry(IRouteEntry routeEntry) => _routeEntries.Add(routeEntry.Uri, routeEntry);
    }
}
