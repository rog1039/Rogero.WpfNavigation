using System.Windows;
using Optional;
using Rogero.WpfNavigation.ExtensionMethods;

namespace Rogero.WpfNavigation.ViewportAdapters;

/// <summary>
/// Provides an interface to UI elements (Window,TabControl,ContentControl,DockLayoutManager, etc)
/// for view/routing management. We can use this interface to add new content to UI elements and
/// we also have the ability to activate views and also to close views.
/// </summary>
public interface IControlViewportAdapter
{
    void                     AddControl(UIElement control, RouteWorkflowTask routeWorkflowTask);
    IList<RouteWorkflowTask> GetActiveRouteWorkflows();
    void                     Activate(RouteWorkflowTask    activeRouteWorkflow);
    void                     CloseScreen(RouteWorkflowTask workflow);
    
    Option<UIElement>        ActiveControl     { get; }
    Option<object>           ActiveDataContext { get; }
    UIElement                ViewportUIElement { get; set; }
}

public abstract class ControlViewportAdapterBase : IControlViewportAdapter
{

    public Option<object> ActiveDataContext
    {
        get
        {
            return ActiveControl
                .Cast<UIElement,FrameworkElement>()
                .Map(frameworkElement => frameworkElement.DataContext);
        }
    }

    public         UIElement         ViewportUIElement { get; set; }
    public virtual Option<UIElement> ActiveControl     { get; }
        
    public abstract IList<RouteWorkflowTask> GetActiveRouteWorkflows();
    public abstract void                     Activate(RouteWorkflowTask    activeRouteWorkflow);
    public abstract void                     CloseScreen(RouteWorkflowTask workflow);

    public abstract void AddControl(UIElement control, RouteWorkflowTask routeWorkflowTask);
}