namespace Rogero.WpfNavigation;

public enum RouteResultStatusCode
{
    OK                = 200,
    Unauthorized      = 401,
    Forbidden         = 403,
    RouteNotFound     = 404,
    RequestTimeout    = 408,
    CanDeactiveFailed = 1100,
    CanActivateFailed = 1101,
    NoViewportFound   = 1102
}