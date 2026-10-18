using System;
using System.Windows;
using System.Windows.Interop;
using Autodesk.Revit.UI;
using RevitVolumeCalc.Core;

namespace RevitVolumeCalc.UI
{
    /// <summary>
    /// Держит единственный экземпляр немодального окна и связанный с ним ExternalEvent.
    /// Все методы вызываются в главном потоке Revit (из команды или из IExternalEventHandler.Execute),
    /// в том же потоке, где создано окно.
    /// </summary>
    internal static class VolumeWindowHost
    {
        private static VolumeWindow _window;
        private static ExternalEvent _externalEvent;

        public static void ShowReport(UIApplication uiApplication, VolumeReport report)
        {
            if (_window == null)
            {
                // ExternalEvent.Create должен вызываться в контексте Revit API — здесь мы внутри команды.
                var handler = new RevitRequestHandler();
                _externalEvent = ExternalEvent.Create(handler);

                var window = new VolumeWindow(handler, _externalEvent);
                new WindowInteropHelper(window).Owner = uiApplication.MainWindowHandle;
                window.Closed += OnWindowClosed;
                _window = window;
                window.SetReport(report);
                window.Show();
            }
            else
            {
                _window.SetReport(report);
                _window.SetStatus("Пересчитано по текущему выделению.");
                if (_window.WindowState == WindowState.Minimized)
                {
                    _window.WindowState = WindowState.Normal;
                }
                _window.Activate();
            }
        }

        public static void UpdateReport(VolumeReport report)
        {
            if (_window != null)
            {
                _window.SetReport(report);
            }
        }

        public static void ShowStatus(string text)
        {
            if (_window != null)
            {
                _window.SetStatus(text);
            }
        }

        public static void CloseWindow()
        {
            if (_window != null)
            {
                _window.Close();
            }
        }

        private static void OnWindowClosed(object sender, EventArgs e)
        {
            var closedWindow = sender as VolumeWindow;
            if (closedWindow != null)
            {
                closedWindow.Closed -= OnWindowClosed;
            }

            if (!object.ReferenceEquals(closedWindow, _window))
            {
                return;
            }

            _window = null;
            if (_externalEvent != null)
            {
                _externalEvent.Dispose();
                _externalEvent = null;
            }
        }
    }
}
