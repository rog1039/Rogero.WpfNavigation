using System;

namespace Rogero.WpfNavigation
{
    public interface IRouteAuthorizationResult
    {
        RouteAuthorizationStatus RouteAuthorizationStatus { get; set; }
    }

    public enum RouteAuthorizationStatus
    {
        NotDetermined,
        Authorized,
        Denied
    }

    public class RouteAuthorizationResult : IRouteAuthorizationResult, IEquatable<RouteAuthorizationResult>
    {
        public RouteAuthorizationStatus RouteAuthorizationStatus { get; set; }

        private RouteAuthorizationResult(RouteAuthorizationStatus status) { RouteAuthorizationStatus = status; }

        public static IRouteAuthorizationResult Denied { get; } = new RouteAuthorizationResult(RouteAuthorizationStatus.Denied);
        public static IRouteAuthorizationResult Granted { get; } = new RouteAuthorizationResult(RouteAuthorizationStatus.Authorized);
        public static IRouteAuthorizationResult NotDetermined { get; } = new RouteAuthorizationResult(RouteAuthorizationStatus.NotDetermined);

        #region Equals
        public bool Equals(RouteAuthorizationResult other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return RouteAuthorizationStatus == other.RouteAuthorizationStatus;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RouteAuthorizationResult)obj);
        }

        public override int GetHashCode()
        {
            return (int)RouteAuthorizationStatus;
        }

        public static bool operator ==(RouteAuthorizationResult left, RouteAuthorizationResult right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(RouteAuthorizationResult left, RouteAuthorizationResult right)
        {
            return !Equals(left, right);
        }
        #endregion
    }

    public static class RouteAuthorizationResultExtensions
    {
        public static bool IsDenied(this IRouteAuthorizationResult result) => result.Equals(RouteAuthorizationResult.Denied);
        public static bool IsGranted(this IRouteAuthorizationResult result) => result.Equals(RouteAuthorizationResult.Granted);
    }
}