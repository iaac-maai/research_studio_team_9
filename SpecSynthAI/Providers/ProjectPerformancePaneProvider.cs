using Autodesk.Revit.UI;
using System;
using System.Windows;
using SpecSynthAI.UI;

namespace SpecSynthAI
{
    public class ProjectPerformancePaneProvider
        : IDockablePaneProvider
    {
        public static readonly DockablePaneId PaneId =
            new DockablePaneId(
                new Guid(
                    "A72D9B51-63F4-4C8B-91E2-7D5A6C8F103B"
                )
            );

        private static ProjectPerformancePane? _pane;

        public ProjectPerformancePaneProvider()
        {
            _pane = new ProjectPerformancePane();
        }

        public static void UpdateStatus(string message)
        {
            if (_pane == null)
                return;

            if (_pane.Dispatcher.CheckAccess())
            {
                _pane.UpdateStatus(message);
            }
            else
            {
                _pane.Dispatcher.BeginInvoke(
                    new Action(() =>
                        _pane?.UpdateStatus(message)
                    )
                );
            }
        }

        public void SetupDockablePane(
            DockablePaneProviderData data)
        {
            data.FrameworkElement = _pane;

            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Right
            };
        }
    }
}