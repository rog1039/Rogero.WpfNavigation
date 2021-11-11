namespace Rogero.WpfNavigation;

public interface IViewAware
{
    Task LoadView(object view);
}