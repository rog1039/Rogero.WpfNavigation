using System;

namespace Rogero.WpfNavigation.ExtensionMethods
{
    public static class TypeExtensions
    {
        public static bool IsSameAsOrSubclassOf(this Type type, Type otherType)
        {
            if (type == otherType)
                return true;
            if (type.IsSubclassOf(otherType))
                return true;

            return false;
        }
    }
}