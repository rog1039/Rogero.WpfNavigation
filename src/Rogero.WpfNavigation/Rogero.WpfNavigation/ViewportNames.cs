using System;

namespace Rogero.WpfNavigation
{
    public static class ViewportNames
    {
        public const string NewWindow   = ":newwindow";
        public const string Dialog      = ":dialog";
        public const string ModalDialog = ":modaldialog";
        public const string MainViewport = "MainViewport";

        public static ViewportType GetViewportTypeFromName(string viewportName)
        {
            if (viewportName.Equals(ViewportNames.NewWindow, StringComparison.InvariantCultureIgnoreCase))
            {
                return ViewportType.NewWindow;
            }

            if (viewportName.Equals(ViewportNames.Dialog, StringComparison.InvariantCultureIgnoreCase))
            {
                return ViewportType.Dialog;
            }

            if (viewportName.Equals(ViewportNames.ModalDialog, StringComparison.InvariantCultureIgnoreCase))
            {
                return ViewportType.ModalDialog;
            }

            return ViewportType.NormalViewport;
        }
    }
}