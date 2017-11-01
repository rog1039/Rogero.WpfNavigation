using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using Rogero.Options;
using Serilog;

namespace Rogero.WpfNavigation
{
    public interface IRouterService
    {
        Guid RouterServiceId { get; }

        Task<RouteResult> RouteAsync(string uri, object initData, string viewportName, IPrincipal principal);

        void RegisterViewport(string viewportName, IControlViewportAdapter viewportAdapter);
        Task<bool> CheckForViewport(string viewportName, TimeSpan timeout);
        Option<IControlViewportAdapter> GetControlViewportAdapter(string viewportName);


        Option<UIElement> GetActiveControl(string viewportName);
        Option<object> GetActiveDataContext(string viewportName);
    }

    public class RouterService : IRouterService
    {
        public Guid RouterServiceId { get; } = Guid.NewGuid();

        private readonly ILogger _logger;
        private readonly IRouteEntryRegistry _routeEntryRegistry;
        private readonly IRouteAuthorizationManager _routeAuthorizationManager;

        private readonly IDictionary<string, IControlViewportAdapter> _viewportAdapters = new Dictionary<string, IControlViewportAdapter>();

        public RouterService(IRouteEntryRegistry routeEntryRegistry, IRouteAuthorizationManager routeAuthorizationManager, ILogger logger)
        {
            _routeEntryRegistry = routeEntryRegistry;
            _routeAuthorizationManager = routeAuthorizationManager;
            InternalLogger.LoggerInstance = logger;
            _logger = logger
                .ForContext("Class", "RouterService")
                .ForContext("RouterServiceId", RouterServiceId);
            _logger.Information("RouterService created with Id: {RouterServiceId}", RouterServiceId);
        }

        public void RegisterViewport(string viewportName, IControlViewportAdapter viewportAdapter)
        {
            _viewportAdapters.Add(viewportName, viewportAdapter);
            _logger.Information("Viewport registered with name {ViewportName} and Viewport Adapter type {ViewportAdapterType}", viewportName, viewportAdapter.GetType().FullName);
        }

        public async Task<RouteResult> RouteAsync(string uri, object initData, string viewportName, IPrincipal principal)
        {
            var routeRequest = new RouteRequest(uri, initData, viewportName, principal);
            return await RouteWorkflowTask.Go(routeRequest, _routeEntryRegistry, _routeAuthorizationManager, this, _logger);
        }
        
        public Option<IControlViewportAdapter> GetControlViewportAdapter(string viewportName) => _viewportAdapters.TryGetValue(viewportName);

        public Option<UIElement> GetActiveControl(string viewportName)
        {
            var viewportAdapter = GetControlViewportAdapter(viewportName);
            return viewportAdapter.HasNoValue
                ? Option<UIElement>.None
                : viewportAdapter.Value.ActiveControl;
        }

        public Option<object> GetActiveDataContext(string viewportName)
        {
            var viewportAdapter = GetControlViewportAdapter(viewportName);
            return viewportAdapter.HasNoValue
                ? Option<UIElement>.None
                : viewportAdapter.Value.ActiveDataContext;
        }

        public async Task<bool> CheckForViewport(string viewportName, TimeSpan timeout)
        {
            async Task<bool> FindViewport()
            {
                var start = DateTime.UtcNow;
                TimeSpan Elapsed() => DateTime.UtcNow - start;
                bool viewportExists = false;

                bool TimedOut() => Elapsed() > timeout;

                bool ViewportFound()
                {
                    viewportExists = DoesViewportExist(viewportName);
                    return viewportExists;
                }

                while (!TimedOut() && !ViewportFound())
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                }

                return viewportExists;
            }

            var findViewportTask = Task.Run(FindViewport);
            return await findViewportTask;
        }

        public bool DoesViewportExist(string viewportName) => _viewportAdapters.ContainsKey(viewportName);
    }
}