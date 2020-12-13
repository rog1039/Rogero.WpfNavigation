using Optional;

namespace Rogero.WpfNavigation.ExtensionMethods
{
    public static class OptionExtensionMethods
    {
        public static bool HasNoValue<T>(this Option<T> option) => !option.HasValue;

        public static Option<T2> Cast<T1, T2>(this Option<T1> option)
        {
            return option.Match(
                some: val1 =>
                {
                    if (val1 is T2 val2) return val2.SomeNotNull();
                    return Option.None<T2>();
                },
                none: () => Option.None<T2>());
        }
    }
}