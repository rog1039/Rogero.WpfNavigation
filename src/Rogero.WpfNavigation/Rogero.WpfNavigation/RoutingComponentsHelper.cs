using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Rogero.WpfNavigation.AttachedProperties;
using Rogero.WpfNavigation.EnumerableTrees;

namespace Rogero.WpfNavigation
{
    public static class RoutingComponentsHelper
    {
        private static readonly TimeSpan DelayBetweenAncestorsWalks = TimeSpan.FromMilliseconds(50);

        public static void HookupViewportToRouterService(DependencyObject viewportControl)
        {
            var dispatcher = Application.Current?.Dispatcher;
            dispatcher?.DelayInvoke(
                TimeSpan.Zero,
                () => FindAndRegisterViewportToRouterService(dispatcher, viewportControl));
        }

        private static void FindAndRegisterViewportToRouterService(Dispatcher dispatcher, DependencyObject d)
        {
            var service = WalkAncestorsForRouterService(d);
            if (service == null)
            {
                RoutingComponentsHelperLogHelper.LogRouterServiceNotFoundMessage();

                dispatcher.DelayInvoke(
                    DelayBetweenAncestorsWalks,
                    () => FindAndRegisterViewportToRouterService(dispatcher, d));
            }
            else
            {
                RoutingComponentsHelperLogHelper.LogRouterServiceFoundMessage(service);
                RegisterViewportWithService(service, d as FrameworkElement);
            }
        }

        private static RouterService WalkAncestorsForRouterService(DependencyObject dependencyObject)
        {
            var routerService = from ancestor in dependencyObject.AncestorsAndSelf()
                let service = RoutingComponent.GetRouterService(ancestor)
                where service != null
                select service;
            return routerService.LastOrDefault();
        }

        private static void RegisterViewportWithService(RouterService service, FrameworkElement control)
        {
            var viewportName = RoutingComponent.GetViewportName(control);
            var viewportAdapterOption = ControlViewportAdapterFactory.GetControlViewportAdapter(control);

            viewportAdapterOption.Match(
                some: viewportAdapter =>
                {
                    RoutingComponentsHelperLogHelper.LogRegisteringViewportMessage(service, control, viewportName);

                    service.RegisterViewport(viewportName, viewportAdapter);

                    RoutingComponentsHelperLogHelper.LogViewportRegisteredMessage(service, control, viewportName);

                },
                none: () =>
                {
                    RoutingComponentsHelperLogHelper.LogViewportAdapterNotFoundMessage(service, control);
                });
        }
    }

    public class RoutingComponentsHelperLogHelper
    {
        public static void LogViewportAdapterNotFoundMessage(RouterService service, FrameworkElement control)
        {
            service._logger
                .ForContext(SerilogConstants.Serilog_SourceContext_Name, "RoutingComponentsHelper")
                .ForContext("RouterServiceId", service.RouterServiceId)
                .Warning(
                    "Unable to register viewport with router service. Viewport adapter not found for control of type {ViewportControlType}",
                    control.GetType());
        }

        public static void LogViewportRegisteredMessage(RouterService service, FrameworkElement control, string viewportName)
        {
            service._logger
                .ForContext(SerilogConstants.Serilog_SourceContext_Name, "RoutingComponentsHelper")
                .Information(
                    "RouterService found. Registered {ViewportName}, of type {ViewportType}, with router service {RouterServiceId}",
                    viewportName, control.GetType(), service.RouterServiceId);
        }

        public static void LogRegisteringViewportMessage(RouterService service, FrameworkElement control, string viewportName)
        {
            service._logger
                .ForContext(SerilogConstants.Serilog_SourceContext_Name, "RoutingComponentsHelper")
                .Information(
                    "RouterService found. Registering {ViewportName}, of type {ViewportType}, with router service {RouterServiceId}",
                    viewportName, control.GetType(), service.RouterServiceId);
        }

        public static void LogRouterServiceFoundMessage(RouterService service)
        {
            service._logger
                .ForContext(SerilogConstants.Serilog_SourceContext_Name, "RoutingComponentsHelper")
                .Debug(
                    "WalkAncestorsForRouterService.WalkAncestorsForRouterService found RouterService with router service id: {RouterServiceId}",
                    service.RouterServiceId);
        }

        public static void LogRouterServiceNotFoundMessage()
        {
            InternalLogger.LoggerInstance
                .ForContext(SerilogConstants.Serilog_SourceContext_Name, "RoutingComponentsHelper")
                .Debug(
                    "WalkAncestorsForRouterService.WalkAncestorsForRouterService did not find the router service. Going to attempt again.");
        }
    }
}