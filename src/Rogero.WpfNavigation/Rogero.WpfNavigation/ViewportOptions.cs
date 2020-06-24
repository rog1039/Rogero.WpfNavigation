using System.Windows;

namespace Rogero.WpfNavigation
{
    public abstract class ViewportOptions
    {
        public static StandardViewportOptions MainViewport() => new StandardViewportOptions()
        {
            Name = ViewportNames.MainViewport
        };

        public static NewWindowViewportOptions NewWindow(Size windowSize) => new NewWindowViewportOptions()
        {
            DesiredWindowSize = windowSize
        };
    }

    public class StandardViewportOptions : ViewportOptions
    {
        public string Name { get; set; }

        public StandardViewportOptions() { }
        public StandardViewportOptions(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            return $"StandardViewportOptions: Name={Name}";
        }
    }

    public class NewWindowViewportOptions : ViewportOptions
    {
        public Size DesiredWindowSize { get; set; }

        public override string ToString()
        {
            return $"NewWindowViewportOptions: Size={DesiredWindowSize}";
        }
    }
}