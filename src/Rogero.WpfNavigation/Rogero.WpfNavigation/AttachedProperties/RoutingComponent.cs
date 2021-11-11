using System.Windows;

namespace Rogero.WpfNavigation.AttachedProperties;

public partial class RoutingComponent
{
    #region RouterService

    /// <summary>
    /// RouterService Attached Dependency Property
    /// </summary>
    public static readonly DependencyProperty RouterServiceProperty =
        DependencyProperty.RegisterAttached("RouterService", typeof(RouterService), typeof(RoutingComponent),
                                            new FrameworkPropertyMetadata((RouterService) null,
                                                                          FrameworkPropertyMetadataOptions
                                                                              .AffectsArrange |
                                                                          FrameworkPropertyMetadataOptions
                                                                              .AffectsMeasure |
                                                                          FrameworkPropertyMetadataOptions
                                                                              .AffectsRender,
                                                                          new PropertyChangedCallback(
                                                                              OnRouterServiceChanged)));

    /// <summary>
    /// Gets the RouterService property. This dependency property 
    /// indicates ....
    /// </summary>
    public static RouterService GetRouterService(DependencyObject d)
    {
        return (RouterService) d.GetValue(RouterServiceProperty);
    }

    /// <summary>
    /// Sets the RouterService property. This dependency property 
    /// indicates ....
    /// </summary>
    public static void SetRouterService(DependencyObject d, RouterService value)
    {
        d.SetValue(RouterServiceProperty, value);
    }

    /// <summary>
    /// Handles changes to the RouterService property.
    /// </summary>
    private static void OnRouterServiceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var oldRouterService = (RouterService) e.OldValue;
        var newRouterService = (RouterService) d.GetValue(RouterServiceProperty);
        Console.WriteLine(newRouterService);
    }

    #endregion
}

public partial class RoutingComponent
{ 

    #region ViewportName

    /// <summary>
    /// ViewportName Attached Dependency Property
    /// </summary>
    public static readonly DependencyProperty ViewportNameProperty =
        DependencyProperty.RegisterAttached("ViewportName", typeof(string), typeof(RoutingComponent),
                                            new FrameworkPropertyMetadata((string)"DefaultName",
                                                                          FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                                                                          new PropertyChangedCallback(OnViewportNameChanged)));

    /// <summary>
    /// Gets the ViewportName property. This dependency property 
    /// indicates ....
    /// </summary>
    public static string GetViewportName(DependencyObject d)
    {
        return (string)d.GetValue(ViewportNameProperty);
    }

    /// <summary>
    /// Sets the ViewportName property. This dependency property 
    /// indicates ....
    /// </summary>
    public static void SetViewportName(DependencyObject d, string value)
    {
        d.SetValue(ViewportNameProperty, value);
    }

    /// <summary>
    /// Handles changes to the ViewportName property.
    /// </summary>
    private static void OnViewportNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var oldViewportName = (string)e.OldValue;
        var newViewportName = (string)d.GetValue(ViewportNameProperty);
        InternalLogger.Information($"RoutingComponent.ViewportName attached property assigned value: {newViewportName}");
        RoutingComponentsHelper.HookupViewportToRouterService(d);
    }

    #endregion
}