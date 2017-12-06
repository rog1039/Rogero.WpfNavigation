using System.Collections.Generic;

namespace Rogero.WpfNavigation.UnitTests.Extensions
{
    public static class ObjectExtensions
    {
        public static List<T> MakeList<T>(this T obj) => new List<T>() {obj};
    }
}
