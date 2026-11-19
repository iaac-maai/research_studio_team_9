using Autodesk.Revit.UI;
using SpecSynthAI.UI;
using SpecSynthAI.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace SpecSynthAI
{
    public class ComplianceModePaneProvider : IDockablePaneProvider
    {
        // Keep the existing GUID so existing references remain valid.
        public static readonly DockablePaneId PaneId =
            new DockablePaneId(
                new Guid("6F6E9A42-7E23-4C66-9E15-2C8A1F0B7D31"));

        private static ComplianceModePane? _compliancePane;
        private static ProjectPerformancePane? _performancePane;
        private static TabControl? _tabControl;

        public ComplianceModePaneProvider()
        {
            _compliancePane = new ComplianceModePane();
            _performancePane = new ProjectPerformancePane();

            CreateSharedPane();
        }

        // ==========================================
        // CREATE SHARED DOCKABLE PANE
        // ==========================================

        private static void CreateSharedPane()
        {
            _tabControl = new TabControl
            {
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0)
            };

            _tabControl.Template = (ControlTemplate)XamlReader.Parse(@"
            <ControlTemplate
                xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                TargetType='{x:Type TabControl}'>

                <Grid Background='White' ClipToBounds='True'>
                    <Grid.RowDefinitions>
                        <RowDefinition Height='Auto'/>
                        <RowDefinition Height='*'/>
                    </Grid.RowDefinitions>

                    <TabPanel
                        Grid.Row='0'
                        IsItemsHost='True'
                        Background='White'/>

                    <Border Grid.Row='1' Background='White'>
                        <ContentPresenter
                            ContentSource='SelectedContent'
                            HorizontalAlignment='Stretch'
                            VerticalAlignment='Stretch'/>
                    </Border>
                </Grid>
            </ControlTemplate>");

            // COMPLIANCE TAB
            var complianceTab = new TabItem
            {
                Header = "Compliance",
                Content = _compliancePane,
                Background = System.Windows.Media.Brushes.White
            };

            // PERFORMANCE TAB
            var performanceTab = new TabItem
            {
                Header = "Performance Dashboard",
                Content = _performancePane,
                Background = System.Windows.Media.Brushes.White
            };

            // IMPORTANT: Add both tabs to the TabControl
            _tabControl.Items.Add(complianceTab);
            _tabControl.Items.Add(performanceTab);

            // Open Compliance by default
            _tabControl.SelectedIndex = 0;
        }

        // ==========================================
        // TAB NAVIGATION
        // ==========================================

        public static void SelectComplianceTab()
        {
            if (_tabControl == null)
                return;

            RunOnUIThread(() =>
            {
                _tabControl.SelectedIndex = 0;
            });
        }

        public static void SelectPerformanceTab()
        {
            if (_tabControl == null)
                return;

            RunOnUIThread(() =>
            {
                _tabControl.SelectedIndex = 1;
            });
        }

        private static void RunOnUIThread(Action action)
        {
            if (_tabControl == null)
                return;

            if (_tabControl.Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                _tabControl.Dispatcher.BeginInvoke(action);
            }
        }

        // ==========================================
        // COMPLIANCE MODE UPDATES
        // Existing functionality preserved
        // ==========================================

        public static void UpdateState(
            bool active,
            int pass = 0,
            int fail = 0,
            int warning = 0,
            int cannotVerify = 0,
            string viewName = "")
        {
            if (_compliancePane == null)
                return;

            RunOnUIThread(() =>
                _compliancePane.SetModeState(
                    active,
                    pass,
                    fail,
                    warning,
                    cannotVerify,
                    viewName));
        }

        public static void UpdateSelection(string message)
        {
            if (_compliancePane == null)
                return;

            RunOnUIThread(() =>
                _compliancePane.SetSelectionInfo(message));
        }

        public static void UpdateInspection(
            WallInspectionData inspection)
        {
            if (_compliancePane == null)
                return;

            RunOnUIThread(() =>
                _compliancePane.SetInspectionData(inspection));
        }

        public static void UpdateRecoveryState(bool hasRecovery)
        {
            if (_compliancePane == null)
                return;

            RunOnUIThread(() =>
                _compliancePane.SetRecoveryState(hasRecovery));
        }

        // ==========================================
        // PERFORMANCE DASHBOARD UPDATES
        // ==========================================

        public static void UpdatePerformanceStatus(string message)
        {
            if (_performancePane == null)
                return;

            RunOnUIThread(() =>
                _performancePane.UpdateStatus(message));
        }

        // ==========================================
        // REVIT DOCKABLE PANE REGISTRATION
        // ==========================================

        public void SetupDockablePane(
            DockablePaneProviderData data)
        {
            data.FrameworkElement = _tabControl;

            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Right
            };
        }
    }
}