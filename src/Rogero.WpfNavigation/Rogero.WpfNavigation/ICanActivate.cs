namespace Rogero.WpfNavigation;

public interface ICanActivate
{
    Task<bool> CanActivate(string uri, object initData);
}