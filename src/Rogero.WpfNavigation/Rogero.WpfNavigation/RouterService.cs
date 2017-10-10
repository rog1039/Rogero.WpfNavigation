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

        public async Task<RouteResult> RouteAsync(string uri, object initData, string viewportName = "") => await RouteWorkflowTask.Go(uri, initData, viewportName, this);

        internal Option<IControlViewportAdapter> GetControlViewportAdapter(string viewportName) => _viewportAdapters.TryGetValue(viewportName);

        internal Option<ViewVmPair> GetViewVmPair(string uri, object initData) => _routeRegistry.FindViewVm(uri, initData);

        public bool DoesViewportExist(string viewportName) => _viewportAdapters.ContainsKey(viewportName);

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


                //bool KeepLookingForViewport()
                //{
                //    if (Elapsed() > timeout) return false;
                //    var viewportExists = DoesViewportExist(viewportName);
                //    var keepLooking = viewportExists ? false : true;
                //    return keepLooking;
                //}

                //var stillLooking = true;
                //while (stillLooking = KeepLookingForViewport())
                //{
                //    await Task.Delay(TimeSpan.FromMilliseconds(100));
                //}

                //var viewportFound = !stillLooking;
                //return viewportFound;
            }

            var findViewportTask = Task.Run(FindViewport);
            return await findViewportTask;
        }
    }
}