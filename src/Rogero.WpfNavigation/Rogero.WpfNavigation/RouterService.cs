using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Rogero.Options;
using Serilog;

namespace Rogero.WpfNavigation
{
    public class RouterService
    {
        private readonly RouteRegistry _registry;
        private readonly ILogger _logger;
        private readonly IDictionary<string, IControlViewportAdapter> _viewportAdapters = new Dictionary<string, IControlViewportAdapter>();

        public RouterService(RouteRegistry registry, ILogger logger)
        {
            _registry = registry;
            _logger = logger;
        }

        public void RegisterViewport(string viewportName, IControlViewportAdapter viewportAdapter)
            => _viewportAdapters.Add(viewportName, viewportAdapter);

        public async Task<RouteResult> RouteAsync(string uri, object initData, string viewportName = "")
        {
            var routeRequestGuid = new Guid();
            var viewVmPair = GetViewVmPair(uri, initData);
            if (viewVmPair.HasNoValue) return new RouteResult(RouteResultStatusCode.RouteNotFound);

            var canDeactivate = await CanDeactivateCurrentRoute(uri, initData);
            if (!canDeactivate) return new RouteResult(RouteResultStatusCode.CanDeactiveFailed);

            var canActivate = await CanActivateNewRoute(uri, initData);
            if (!canActivate) return new RouteResult(RouteResultStatusCode.CanActivateFailed);

            var viewModel = GetViewModel(uri, initData, viewVmPair.Value);
            InitializeViewModel(uri, initData, viewModel);
            var view = GetView(uri, initData, viewVmPair.Value);
            AssignDataContext(view, viewModel);

            return AddViewToUi(viewportName, view);
        }

        private void InitializeViewModel(string uri, object initData, object viewModel)
        {
            var initMethod = viewModel.GetType().GetMethod("Init");
            if (initMethod == null) return;
            initMethod.Invoke(viewModel, new[] { initData });
        }

        private RouteResult AddViewToUi(string viewportName, UIElement view)
        {
            var viewport = _viewportAdapters.TryGetValue(viewportName);
            if (viewport.HasValue)
            {
                viewport.Value.AddControl(view);
                return RouteResult.Succeeded;
            }
            else
            {
                //TODO: Report no viewport found somehow!
                return new RouteResult(RouteResultStatusCode.NoViewportFound);
            }
        }

        private void AssignDataContext(object view, object viewModel)
        {
            if (view is FrameworkElement)
            {
                var fe = (FrameworkElement)view;
                fe.DataContext = viewModel;
            }
        }

        private Option<ViewVmPair> GetViewVmPair(string uri, object initData)
        {
            var pair = _registry.FindViewVm(uri, initData);
            return pair;
        }

        private UIElement GetView(string uri, object initData, ViewVmPair value)
        {
            return value.CreateView();
        }

        private object GetViewModel(string uri, object initData, ViewVmPair value)
        {
            return value.CreateViewModel();
        }

        private Task<bool> CanActivateNewRoute(string uri, object initData)
        {
            return Task.FromResult(true);
        }

        private object CurrentViewModel => new NotImplementedException();

        private async Task<bool> CanDeactivateCurrentRoute(string uri, object initData)
        {
            var currentViewModel = CurrentViewModel;
            var canDeactivate = currentViewModel as ICanDeactivate;
            if (canDeactivate != null)
            {
                return await canDeactivate.CanDeactivate(uri, initData);
            }
            return true;
        }
    }
}