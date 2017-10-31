using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Rogero.Options;
using Serilog;

namespace Rogero.WpfNavigation
{
    public interface IRouterService
    {
        Guid RouterServiceId { get; }

        Task<RouteResult> RouteAsync(string uri, object initData, string viewportName = "");

        void RegisterViewport(string viewportName, IControlViewportAdapter viewportAdapter);
        Task<bool> CheckForViewport(string viewportName, TimeSpan timeout);
        Option<IControlViewportAdapter> GetControlViewportAdapter(string viewportName);


        Option<UIElement> GetActiveControl(string viewportName);
        Option<object> GetActiveDataContext(string viewportName);
    }

    public class RouterService : IRouterService
    {
        public Guid RouterServiceId { get; } = Guid.NewGuid();

        internal readonly ILogger Logger;

        private readonly RouteRegistry _routeRegistry;
        private readonly IDictionary<string, IControlViewportAdapter> _viewportAdapters = new Dictionary<string, IControlViewportAdapter>();

        public RouterService(RouteRegistry routeRegistry, ILogger logger)
        {
            _routeRegistry = routeRegistry;
            InternalLogger.LoggerInstance = logger;
            Logger = logger
                .ForContext("Class", "RouterService")
                .ForContext("RouterServiceId", RouterServiceId);
            Logger.Information("RouterService created with Id: {RouterServiceId}", RouterServiceId);
        }

        public void RegisterViewport(string viewportName, IControlViewportAdapter viewportAdapter)
        {
            _viewportAdapters.Add(viewportName, viewportAdapter);
            Logger.Information("Viewport registered with name {ViewportName} and Viewport Adapter type {ViewportAdapterType}", viewportName, viewportAdapter.GetType().FullName);
        }

        public async Task<RouteResult> RouteAsync(string uri, object initData, string viewportName = "")
        {
            return await RouteWorkflowTask.Go(uri, initData, viewportName, this);
        }


        internal Option<ViewVmPair> GetViewVmPair(string uri, object initData) => _routeRegistry.GetViewVmPair(uri, initData);
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