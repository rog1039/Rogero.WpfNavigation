﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace Rogero.WpfNavigation
{
    public class RoutingComponentsHelper
    {
        private static readonly TimeSpan DelayBetweenAncestorsWalks = TimeSpan.FromMilliseconds(50);

        public static void HookupViewportToRouterService(DependencyObject d)
        {
            var dispatcher = Application.Current?.Dispatcher;
            dispatcher?.DelayInvoke(
                TimeSpan.Zero,
                () => FindAndRegisterViewportToRouterService(dispatcher, d));
        }

        private static void FindAndRegisterViewportToRouterService(Dispatcher dispatcher, DependencyObject d)
        {
            var service = WalkAncestorsForRouterService(d);
            if (service == null)
            {
                dispatcher.DelayInvoke(
                    DelayBetweenAncestorsWalks,
                    () => FindAndRegisterViewportToRouterService(dispatcher, d));
            }
            else
            {
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
            var viewportAdapter = ControlViewportAdapterFactory.GetControlViewportAdapter(control);
            if (viewportAdapter.HasValue)
            {
                service.RegisterViewport(viewportName, viewportAdapter.Value);
                InternalLogger.Information("Service found! Registering {viewportName}, of type {ViewportType} with router service {service}", viewportName, control.GetType(), service.RouterServiceId);
            }
            else
            {
                InternalLogger.Warning("Unable to register viewport with router service. Viewport adapter not found for control of type {ViewportControlType}",
                                       control.GetType());
            }
        }
    }
}