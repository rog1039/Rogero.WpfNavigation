﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Optional;
using Optional.Collections;

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
    
    public interface IRouteEntryRegistry
    {
        Guid Id { get; }
        void RegisterRouteEntry(IRouteEntry routeEntry);
        Option<IRouteEntry> GetRouteEntry(string uri);
        IList<IRouteEntry> GetRouteEntries();
    }

    public class RouteEntryRegistry : IRouteEntryRegistry
    {
        public Guid Id { get; } = Guid.NewGuid();

        private readonly IDictionary<string, IRouteEntry> _routeEntries = new ConcurrentDictionary<string, IRouteEntry>();

        public RouteEntryRegistry()
        {
            
        }

        public Option<IRouteEntry> GetRouteEntry(string uri) => _routeEntries.GetValueOrNone(uri);

        public IList<IRouteEntry> GetRouteEntries()
        {
            return _routeEntries.Values.ToList();
        }

        public void RegisterRouteEntry(IRouteEntry routeEntry)
        {
            try
            {
                _routeEntries.Add(routeEntry.Uri, routeEntry);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
