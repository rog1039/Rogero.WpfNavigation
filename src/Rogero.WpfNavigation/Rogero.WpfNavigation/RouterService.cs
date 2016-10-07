using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rogero.Options;
using Serilog;

namespace Rogero.WpfNavigation
{
    public class RouterService
    {
        public Guid RouterServiceId { get; } = Guid.NewGuid();

        internal readonly ILogger _logger;

        private readonly RouteRegistry _routeRegistry;
        private readonly IDictionary<string, IControlViewportAdapter> _viewportAdapters = new Dictionary<string, IControlViewportAdapter>();

        public RouterService(RouteRegistry routeRegistry, ILogger logger)
        {
            _routeRegistry = routeRegistry;
            _logger = logger.ForContext("Class", "RouterService");
            _logger.Information("RouterService created with Id: {RouterServiceId}", RouterServiceId);
        }

        public void RegisterViewport(string viewportName, IControlViewportAdapter viewportAdapter)
        {
            _viewportAdapters.Add(viewportName, viewportAdapter);
            _logger.Information("Viewport registered with name {ViewportName} and Viewport Adapter type {ViewportAdapterType}", viewportName, viewportAdapter.GetType().FullName);
        }

        public async Task<RouteResult> RouteAsync(string uri, object initData, string viewportName = "") => await RouteWorkflowTask.Go(uri, initData, viewportName, this);

        internal Option<IControlViewportAdapter> GetControlViewportAdapter(string viewportName) => _viewportAdapters.TryGetValue(viewportName);

        internal Option<ViewVmPair> GetViewVmPair(string uri, object initData) => _routeRegistry.FindViewVm(uri, initData);

        public bool DoesViewportExist(string viewportName) => _viewportAdapters.ContainsKey(viewportName);
    }
}