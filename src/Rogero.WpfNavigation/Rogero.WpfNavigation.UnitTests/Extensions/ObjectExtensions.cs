using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rogero.WpfNavigation.UnitTests.Extensions
{
    public static class ObjectExtensions
    {
        public static List<T> MakeList<T>(this T obj) => new List<T>() {obj};
    }
}
