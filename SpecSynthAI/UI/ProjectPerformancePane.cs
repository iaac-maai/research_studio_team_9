using Microsoft.Win32;

using SpecSynthAI.Events;
using SpecSynthAI.Services;
using SpecSynthAI.AssemblyOptimisation.Models.MultiObjective.Integration;
using SpecSynthAI.AssemblyOptimisation.Services.MultiObjective.Integration;

using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using ThermalEngine.Configuration;
using ThermalEngine.Models;
using ThermalEngine.Services;
using ThermalEngine.UI.Services;
using ThermalEngine.UI.ViewModels;
using ThermalEngine.UI.Views;

namespace SpecSynthAI.UI
{
    public partial class ProjectPerformancePane : UserControl
    {
        private readonly TextBlock _statusText;

        // Multi-objective optimisation results
        private readonly StackPanel _optimisationResultsHost;

        // User-selected optimisation objectives
        private readonly CheckBox _objectiveUValueCheckBox;
        private readonly CheckBox _objectiveCarbonCheckBox;
        private readonly CheckBox _objectiveCostCheckBox;

        // Optimisation run history
        private readonly OptimisationRunHistoryService _runHistoryService;
        private readonly ListBox _runHistoryList;
        private readonly TextBlock _runHistoryStatusText;

        // Optimisation run comparison
        private readonly OptimisationRunComparisonService _runComparisonService;
        private readonly ComboBox _compareRunACombo;
        private readonly ComboBox _compareRunBCombo;
        private readonly StackPanel _runComparisonHost;

        private readonly DashboardViewModel _viewModel;
        private readonly ProjectWallJsonImportService _importService;
        private readonly ProjectOverviewView _projectOverviewView;
        private readonly Expander _projectOverviewExpander;

        // KPI values
        private readonly TextBlock _totalWallsValue;
        private readonly TextBlock _averageUValueValue;
        private readonly TextBlock _compliantValue;
        private readonly TextBlock _needsReviewValue;

        // Distribution values
        private readonly TextBlock _goodCount;
        private readonly TextBlock _moderateCount;
        private readonly TextBlock _poorCount;
        private readonly TextBlock _notAnalysedCount;
        private readonly TextBlock _unsupportedCount;

        private readonly Border _goodBar;
        private readonly Border _moderateBar;
        private readonly Border _poorBar;
        private readonly Border _notAnalysedBar;
        private readonly Border _unsupportedBar;

        // Project insights
        private readonly TextBlock _bestAssemblyText;
        private readonly TextBlock _worstAssemblyText;
        private readonly TextBlock _improvementText;
        private readonly TextBlock _dataQualityText;

        // Selected wall thermal analysis
        private readonly TextBlock _selectedWallNameText;
        private readonly TextBlock _selectedWallUValueText;
        private readonly TextBlock _selectedWallResistanceText;
        private readonly StackPanel _selectedWallLayersPanel;
        private readonly StackPanel _selectedWallDiagnosticsPanel;
        private readonly StackPanel _selectedWallQualityPanel;

        private const double DistributionBarWidth = 300;

        // =========================================
        // COLOUR HELPER
        // =========================================

        private static Brush BrushFromHex(string hex)
        {
            return new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(hex)!);
        }

        // =========================================
        // CONSTRUCTOR
        // =========================================

        public ProjectPerformancePane()
        {
            MinWidth = 320;
            Background = BrushFromHex("#F4F6F8");

            // Initialise services
            _viewModel = new DashboardViewModel();

            _importService = new ProjectWallJsonImportService();

            _runHistoryService = new OptimisationRunHistoryService();
            _runComparisonService = new OptimisationRunComparisonService();

            // Initialise project overview
            _projectOverviewView = new ProjectOverviewView
            {
                DataContext = _viewModel
            };

            _projectOverviewExpander = new Expander
            {
                Header = "PROJECT MODEL OVERVIEW",
                IsExpanded = false,
                Content = _projectOverviewView,
                Background = Brushes.White,
                Foreground = BrushFromHex("#172B4D"),
                Margin = new Thickness(0, 0, 0, 14)
            };

            var root = new DockPanel
            {
                LastChildFill = true,
                Background = BrushFromHex("#F4F6F8")
            };

            // =====================================
            // HEADER
            // =====================================

            var header = new Border
            {
                Background = BrushFromHex("#172B4D"),
                Padding = new Thickness(18, 20, 18, 20)
            };

            var headerStack = new StackPanel();

            headerStack.Children.Add(new TextBlock
            {
                Text = "SPECSYNTH AI",
                FontSize = 19,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            });

            headerStack.Children.Add(new TextBlock
            {
                Text = "THERMAL PERFORMANCE",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = BrushFromHex("#70E1C1"),
                Margin = new Thickness(0, 6, 0, 0)
            });

            headerStack.Children.Add(new TextBlock
            {
                Text = "Building Performance Intelligence",
                FontSize = 11,
                Foreground = BrushFromHex("#D6DFEA"),
                Margin = new Thickness(0, 8, 0, 0)
            });

            header.Child = headerStack;

            DockPanel.SetDock(header, Dock.Top);
            root.Children.Add(header);

            // =====================================
            // MAIN BODY
            // =====================================

            var body = new StackPanel
            {
                Margin = new Thickness(16)
            };

            body.Children.Add(new TextBlock
            {
                Text = "PROJECT PERFORMANCE",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = BrushFromHex("#172B4D"),
                Margin = new Thickness(0, 0, 0, 14)
            });

            // PROJECT OVERVIEW
            body.Children.Add(_projectOverviewExpander);

            // =====================================
            // KPI CARDS
            // =====================================

            body.Children.Add(CreateKpiCard(
                "TOTAL WALLS",
                "—",
                "Walls in active project",
                "#172B4D",
                out _totalWallsValue
            ));

            body.Children.Add(CreateKpiCard(
                "AVERAGE U-VALUE",
                "—",
                "W/(m²·K)",
                "#2685A6",
                out _averageUValueValue
            ));

            body.Children.Add(CreateKpiCard(
                "COMPLIANT",
                "—",
                "Walls passing project targets",
                "#269B70",
                out _compliantValue
            ));

            body.Children.Add(CreateKpiCard(
                "NEEDS IMPROVEMENT",
                "—",
                "Walls with poor thermal performance",
                "#D99A32",
                out _needsReviewValue
            ));

            // =====================================
            // PERFORMANCE DISTRIBUTION
            // =====================================

            body.Children.Add(new TextBlock
            {
                Text = "PERFORMANCE DISTRIBUTION",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = BrushFromHex("#172B4D"),
                Margin = new Thickness(0, 22, 0, 6)
            });

            body.Children.Add(new TextBlock
            {
                Text = "Thermal performance across project wall types",
                FontSize = 10,
                Foreground = BrushFromHex("#667085"),
                Margin = new Thickness(0, 0, 0, 14)
            });

            var distributionCard = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(8),
                BorderBrush = BrushFromHex("#DCE3EA"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14)
            };

            var distributionStack = new StackPanel();

            distributionStack.Children.Add(
                CreateDistributionRow(
                    "Good performance",
                    "#269B70",
                    out _goodCount,
                    out _goodBar
                )
            );

            distributionStack.Children.Add(
                CreateDistributionRow(
                    "Moderate performance",
                    "#D99A32",
                    out _moderateCount,
                    out _moderateBar
                )
            );

            distributionStack.Children.Add(
                CreateDistributionRow(
                    "Poor performance",
                    "#D9534F",
                    out _poorCount,
                    out _poorBar
                )
            );

            distributionStack.Children.Add(
                CreateDistributionRow(
                    "Not analysed",
                    "#98A2B3",
                    out _notAnalysedCount,
                    out _notAnalysedBar
                )
            );

            distributionStack.Children.Add(
                CreateDistributionRow(
                    "Unsupported",
                    "#667085",
                    out _unsupportedCount,
                    out _unsupportedBar
                )
            );

            distributionCard.Child = distributionStack;
            body.Children.Add(distributionCard);

            // =====================================
            // PROJECT INSIGHTS
            // =====================================

            body.Children.Add(new TextBlock
            {
                Text = "PROJECT INSIGHTS",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = BrushFromHex("#172B4D"),
                Margin = new Thickness(0, 24, 0, 6)
            });

            body.Children.Add(new TextBlock
            {
                Text = "Key findings from thermal performance analysis",
                FontSize = 10,
                Foreground = BrushFromHex("#667085"),
                Margin = new Thickness(0, 0, 0, 14)
            });

            body.Children.Add(CreateInsightCard(
                "BEST-PERFORMING ASSEMBLY",
                "Awaiting project analysis.",
                "#269B70",
                out _bestAssemblyText
            ));

            body.Children.Add(CreateInsightCard(
                "LOWEST-PERFORMING ASSEMBLY",
                "Awaiting project analysis.",
                "#D9534F",
                out _worstAssemblyText
            ));

            body.Children.Add(CreateInsightCard(
                "IMPROVEMENT OPPORTUNITIES",
                "Thermal improvement opportunities will appear here.",
                "#D99A32",
                out _improvementText
            ));

            body.Children.Add(CreateInsightCard(
                "DATA QUALITY",
                "Missing and unverified thermal properties will be identified here.",
                "#2685A6",
                out _dataQualityText
            ));

            // =====================================
            // SELECTED WALL THERMAL ANALYSIS
            // =====================================

            body.Children.Add(new TextBlock
            {
                Text = "SELECTED WALL THERMAL ANALYSIS",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = BrushFromHex("#172B4D"),
                Margin = new Thickness(0, 24, 0, 10)
            });

            var selectedWallCard = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(8),
                BorderBrush = BrushFromHex("#DCE3EA"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var selectedWallLayout = new StackPanel();

            _selectedWallNameText = new TextBlock
            {
                Text = "No wall selected",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = BrushFromHex("#172B4D"),
                TextWrapping = TextWrapping.Wrap
            };

            selectedWallLayout.Children.Add(_selectedWallNameText);

            _selectedWallUValueText = new TextBlock
            {
                Text = "U-value: — W/(m²·K)",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = BrushFromHex("#2685A6"),
                Margin = new Thickness(0, 12, 0, 4)
            };

            selectedWallLayout.Children.Add(_selectedWallUValueText);

            _selectedWallResistanceText = new TextBlock
            {
                Text = "Total resistance: — m²·K/W",
                FontSize = 11,
                Foreground = BrushFromHex("#667085"),
                Margin = new Thickness(0, 0, 0, 12)
            };

            selectedWallLayout.Children.Add(_selectedWallResistanceText);

            selectedWallLayout.Children.Add(new TextBlock
            {
                Text = "LAYER RESISTANCE BREAKDOWN",
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = BrushFromHex("#475467"),
                Margin = new Thickness(0, 4, 0, 8)
            });

            // Three focused tabs keep the selected-wall workspace readable.
            _selectedWallLayersPanel = new StackPanel();
            _selectedWallDiagnosticsPanel = new StackPanel();
            _selectedWallQualityPanel = new StackPanel();

            AddEmptyState(_selectedWallLayersPanel,
                "Select a wall to view its layer resistance breakdown.");
            AddEmptyState(_selectedWallDiagnosticsPanel,
                "Select a wall to view its thermal diagnostics.");
            AddEmptyState(_selectedWallQualityPanel,
                "Select a wall to assess material data quality.");

            var analysisTabs = new TabControl
            {
                Margin = new Thickness(0, 8, 0, 0),
                Background = Brushes.White,
                BorderThickness = new Thickness(0)
            };

            analysisTabs.Items.Add(new TabItem
            {
                Header = "LAYERS",
                Content = CreateTabContent(_selectedWallLayersPanel)
            });
            analysisTabs.Items.Add(new TabItem
            {
                Header = "DIAGNOSTICS",
                Content = CreateTabContent(_selectedWallDiagnosticsPanel)
            });
            analysisTabs.Items.Add(new TabItem
            {
                Header = "DATA QUALITY",
                Content = CreateTabContent(_selectedWallQualityPanel)
            });

            selectedWallLayout.Children.Add(analysisTabs);
            selectedWallCard.Child = selectedWallLayout;
            body.Children.Add(selectedWallCard);

            // =====================================
            // MULTI-OBJECTIVE OPTIMISATION
            // =====================================

            body.Children.Add(new TextBlock
            {
                Text = "MULTI-OBJECTIVE OPTIMISATION",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = BrushFromHex("#172B4D"),
                Margin = new Thickness(0, 24, 0, 6)
            });

            body.Children.Add(new TextBlock
            {
                Text = "Compare material alternatives across thermal performance, embodied carbon and cost.",
                FontSize = 10,
                Foreground = BrushFromHex("#667085"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12)
            });

            // User-defined optimisation objectives.
            var objectiveSelectionCard = new Border
            {
                Background = Brushes.White,
                BorderBrush = BrushFromHex("#DCE3EA"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 12)
            };

            var objectiveSelectionStack = new StackPanel();

            objectiveSelectionStack.Children.Add(new TextBlock
            {
                Text = "SELECT OPTIMISATION OBJECTIVES",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = BrushFromHex("#475467"),
                Margin = new Thickness(0, 0, 0, 8)
            });

            _objectiveUValueCheckBox = new CheckBox
            {
                Content = "Minimise U-value (thermal performance)",
                IsChecked = true,
                Margin = new Thickness(0, 3, 0, 3),
                Foreground = BrushFromHex("#344054")
            };

            _objectiveCarbonCheckBox = new CheckBox
            {
                Content = "Minimise embodied carbon",
                IsChecked = true,
                Margin = new Thickness(0, 3, 0, 3),
                Foreground = BrushFromHex("#344054")
            };

            _objectiveCostCheckBox = new CheckBox
            {
                Content = "Minimise material cost",
                IsChecked = true,
                Margin = new Thickness(0, 3, 0, 3),
                Foreground = BrushFromHex("#344054")
            };

            objectiveSelectionStack.Children.Add(_objectiveUValueCheckBox);
            objectiveSelectionStack.Children.Add(_objectiveCarbonCheckBox);
            objectiveSelectionStack.Children.Add(_objectiveCostCheckBox);

            objectiveSelectionStack.Children.Add(new TextBlock
            {
                Text = "Select at least one objective. All three are selected by default.",
                FontSize = 9,
                Foreground = BrushFromHex("#667085"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0)
            });

            objectiveSelectionCard.Child = objectiveSelectionStack;
            body.Children.Add(objectiveSelectionCard);

            var loadOptimisationButton = new Button
            {
                Content = "Run Multi-Objective Optimisation",
                Height = 38,
                Margin = new Thickness(0, 0, 0, 10),
                Background = BrushFromHex("#2685A6"),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };

            loadOptimisationButton.Click += LoadOptimisationResults_Click;
            body.Children.Add(loadOptimisationButton);

            _optimisationResultsHost = new StackPanel();
            _optimisationResultsHost.Children.Add(new TextBlock
            {
                Text = "No optimisation results loaded.",
                FontSize = 11,
                Foreground = BrushFromHex("#98A2B3"),
                Margin = new Thickness(0, 8, 0, 8)
            });

            body.Children.Add(_optimisationResultsHost);

            // =====================================
            // OPTIMISATION RUN HISTORY
            // =====================================

            body.Children.Add(new TextBlock
            {
                Text = "PREVIOUS OPTIMISATION RUNS",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = BrushFromHex("#172B4D"),
                Margin = new Thickness(0, 16, 0, 8)
            });

            _runHistoryList = new ListBox
            {
                Height = 170,
                Background = Brushes.White,
                BorderBrush = BrushFromHex("#DCE3EA"),
                BorderThickness = new Thickness(1),
                FontSize = 10,
                DisplayMemberPath = nameof(OptimisationRunHistoryItem.DisplayLabel),
                SelectionMode = SelectionMode.Single
            };

            body.Children.Add(_runHistoryList);

            _runHistoryStatusText = new TextBlock
            {
                Text = "History has not been scanned yet.",
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
                Foreground = BrushFromHex("#667085"),
                Margin = new Thickness(0, 6, 0, 4)
            };

            body.Children.Add(_runHistoryStatusText);

            var historyButtons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 8, 0, 12)
            };

            var refreshHistoryButton = new Button
            {
                Content = "Refresh History",
                Height = 32,
                Width = 110,
                Margin = new Thickness(0, 0, 8, 0),
                Background = BrushFromHex("#E8EEF5"),
                Foreground = BrushFromHex("#172B4D"),
                BorderThickness = new Thickness(0)
            };

            refreshHistoryButton.Click += (sender, args) =>
                RefreshOptimisationHistory();

            var loadHistoryButton = new Button
            {
                Content = "Load Selected Run",
                Height = 32,
                Width = 130,
                Background = BrushFromHex("#2685A6"),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };

            loadHistoryButton.Click += LoadSelectedOptimisationRun_Click;

            historyButtons.Children.Add(refreshHistoryButton);
            historyButtons.Children.Add(loadHistoryButton);

            body.Children.Add(historyButtons);

            // =====================================
            // OPTIMISATION RUN COMPARISON
            // =====================================

            body.Children.Add(new TextBlock
            {
                Text = "COMPARE OPTIMISATION RUNS",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = BrushFromHex("#172B4D"),
                Margin = new Thickness(0, 14, 0, 8)
            });

            body.Children.Add(new TextBlock
            {
                Text = "Select two saved runs to compare their Pareto solution sets.",
                FontSize = 10,
                Foreground = BrushFromHex("#667085"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });

            _compareRunACombo = new ComboBox
            {
                Height = 30,
                Margin = new Thickness(0, 0, 0, 6),
                DisplayMemberPath = nameof(OptimisationRunHistoryItem.DisplayLabel)
            };
            body.Children.Add(_compareRunACombo);

            _compareRunBCombo = new ComboBox
            {
                Height = 30,
                Margin = new Thickness(0, 0, 0, 8),
                DisplayMemberPath = nameof(OptimisationRunHistoryItem.DisplayLabel)
            };
            body.Children.Add(_compareRunBCombo);

            var compareRunsButton = new Button
            {
                Content = "Compare Selected Runs",
                Height = 34,
                Width = 175,
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = BrushFromHex("#172B4D"),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 0, 10)
            };
            compareRunsButton.Click += CompareSelectedRuns_Click;
            body.Children.Add(compareRunsButton);

            _runComparisonHost = new StackPanel
            {
                Margin = new Thickness(0, 4, 0, 12)
            };
            _runComparisonHost.Children.Add(new TextBlock
            {
                Text = "Select two different runs and click Compare Selected Runs.",
                FontSize = 10,
                Foreground = BrushFromHex("#667085"),
                TextWrapping = TextWrapping.Wrap
            });
            body.Children.Add(_runComparisonHost);

            // History is loaded from the Loaded event after all
            // dashboard controls, including _statusText, are initialised.

            // =====================================
            // ANALYSIS SECTION
            // =====================================

            body.Children.Add(new TextBlock
            {
                Text = "ANALYSIS",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = BrushFromHex("#172B4D"),
                Margin = new Thickness(0, 24, 0, 12)
            });

            var projectButton = new Button
            {
                Content = "Load Project Overview",
                Height = 38,
                Margin = new Thickness(0, 0, 0, 8),
                Background = BrushFromHex("#172B4D"),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };

            projectButton.Click += ProjectButton_Click;
            body.Children.Add(projectButton);

            var selectionButton = new Button
            {
                Content = "Inspect Selection",
                Height = 38,
                Background = BrushFromHex("#E8EEF5"),
                Foreground = BrushFromHex("#172B4D"),
                BorderThickness = new Thickness(0)
            };

            selectionButton.Click += SelectionButton_Click;
            body.Children.Add(selectionButton);

            // =====================================
            // SYSTEM STATUS
            // =====================================

            body.Children.Add(new TextBlock
            {
                Text = "SYSTEM STATUS",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = BrushFromHex("#172B4D"),
                Margin = new Thickness(0, 22, 0, 8)
            });

            _statusText = new TextBlock
            {
                Text = "Dashboard initialized. Awaiting project data.",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 11,
                Foreground = BrushFromHex("#667085")
            };

            body.Children.Add(_statusText);

            // =====================================
            // FOOTER
            // =====================================

            body.Children.Add(new TextBlock
            {
                Text = "SpecSynth AI | IAAC Research Studio",
                FontSize = 10,
                Foreground = BrushFromHex("#98A2B3"),
                Margin = new Thickness(0, 25, 0, 0)
            });

            // =====================================
            // SCROLL VIEWER
            // =====================================

            var scrollViewer = new ScrollViewer
            {
                Content = body,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            root.Children.Add(scrollViewer);

            Content = root;

            // Wait until the dashboard is fully constructed before
            // refreshing history because RefreshOptimisationHistory
            // also updates the system-status text.
            Loaded += (sender, args) => RefreshOptimisationHistory();
        }

        // =========================================
        // LOAD MULTI-OBJECTIVE OPTIMISATION RESULTS
        // =========================================

        private async void LoadOptimisationResults_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            var selectedObjectives = new System.Collections.Generic.List<string>();

            if (_objectiveUValueCheckBox.IsChecked == true)
                selectedObjectives.Add("minimise_u_value");

            if (_objectiveCarbonCheckBox.IsChecked == true)
                selectedObjectives.Add("minimise_carbon");

            if (_objectiveCostCheckBox.IsChecked == true)
                selectedObjectives.Add("minimise_cost");

            if (selectedObjectives.Count == 0)
            {
                UpdateStatus(
                    "Please select at least one optimisation objective before running.");

                return;
            }

            button.IsEnabled = false;
            button.Content = "Running Optimisation...";

            string selectedSummary = string.Join(", ", selectedObjectives);

            UpdateStatus(
                "Starting controlled Stage 7.2 optimisation.\\n" +
                "Selected objectives: " + selectedSummary + "\\n" +
                "Python engine is running...");

            try
            {
                // Build the request from the user's checkbox selections.
                // Constraints and NSGA-II settings retain their model defaults.
                var request = new OptimisationRequest
                {
                    Project = new ProjectContext
                    {
                        ProjectId = "ACTIVE_REVIT_PROJECT",
                        ProjectName = "SpecSynth AI Project"
                    },
                    Optimisation = new OptimisationSettings
                    {
                        Objectives = selectedObjectives
                    }
                };

                var executionService = new OptimisationExecutionService();

                OptimisationResult result =
                    await executionService.RunAsync(request);

                _optimisationResultsHost.Children.Clear();
                _optimisationResultsHost.Children.Add(
                    new OptimisationResultsView(result));

                RefreshOptimisationHistory();

                int outOfDomainCount = result.Solutions.Count(
                    solution => solution.ModelOutOfDomain);

                string exhaustiveMatch =
                    result.Summary.MatchesExhaustiveReference
                        ? "YES"
                        : "NO / NOT CONFIRMED";

                UpdateStatus(
                    "Optimisation completed successfully.\\n" +
                    $"Request ID: {result.RequestId}\\n" +
                    $"Selected objectives: {selectedSummary}\\n" +
                    $"Candidates: {result.Summary.CandidateCount}\\n" +
                    $"Pareto solutions: {result.Solutions.Count}\\n" +
                    $"Out-of-domain Pareto solutions: {outOfDomainCount}\\n" +
                    $"Exhaustive reference match: {exhaustiveMatch}\\n" +
                    $"Result directory: 05_Research_Experiments\\\\04_Integration\\\\output");
            }
            catch (Exception ex)
            {
                UpdateStatus(
                    "Optimisation execution failed:\\n" + ex.Message);
            }
            finally
            {
                button.IsEnabled = true;
                button.Content = "Run Multi-Objective Optimisation";
            }
        }

        // REFRESH OPTIMISATION RUN HISTORY
        // =========================================

        private void RefreshOptimisationHistory()
        {
            try
            {
                var history = _runHistoryService.GetHistory();

                _runHistoryList.ItemsSource = null;
                _runHistoryList.ItemsSource = history;

                if (history.Count > 0)
                    _runHistoryList.SelectedIndex = 0;

                _compareRunACombo.ItemsSource = null;
                _compareRunBCombo.ItemsSource = null;

                _compareRunACombo.ItemsSource = history;
                _compareRunBCombo.ItemsSource = history;

                if (history.Count > 0)
                    _compareRunACombo.SelectedIndex = 0;

                if (history.Count > 1)
                    _compareRunBCombo.SelectedIndex = 1;

                string scanSummary = _runHistoryService.LastScanSummary;
                var warnings = _runHistoryService.LastScanWarnings;

                if (warnings.Count > 0)
                {
                    string warningPreview = string.Join(
                        "\n• ",
                        warnings.Take(3));

                    if (warnings.Count > 3)
                        warningPreview += $"\n• ...and {warnings.Count - 3} more.";

                    _runHistoryStatusText.Text =
                        $"{scanSummary}\n" +
                        $"Warnings: {warnings.Count}\n• {warningPreview}";

                    _runHistoryStatusText.Foreground = BrushFromHex("#B54708");
                }
                else
                {
                    _runHistoryStatusText.Text = scanSummary;
                    _runHistoryStatusText.Foreground = BrushFromHex("#667085");
                }

                UpdateStatus(
                    history.Count > 0
                        ? $"Optimisation history loaded. {history.Count} valid runs found."
                        : "No complete optimisation runs were found.");
            }
            catch (Exception ex)
            {
                _runHistoryList.ItemsSource = null;
                _runHistoryStatusText.Text = "History scan failed: " + ex.Message;
                _runHistoryStatusText.Foreground = BrushFromHex("#D92D20");

                UpdateStatus(
                    "Unable to load optimisation history:\n" + ex.Message);
            }
        }


        // =========================================
        // LOAD SELECTED HISTORICAL RUN
        // =========================================

        private void LoadSelectedOptimisationRun_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                if (_runHistoryList.SelectedItem
                    is not OptimisationRunHistoryItem selectedRun)
                {
                    UpdateStatus(
                        "Please select an optimisation run first.");

                    return;
                }

                OptimisationResult result =
                    _runHistoryService.LoadResult(selectedRun);

                _optimisationResultsHost.Children.Clear();

                _optimisationResultsHost.Children.Add(
                    new OptimisationResultsView(result));

                UpdateStatus(
                    "Historical optimisation result loaded.\n" +
                    $"Request ID: {result.RequestId}\n" +
                    $"Status: {result.Status}\n" +
                    $"Candidates: {result.Summary.CandidateCount}\n" +
                    $"Pareto solutions: {result.Solutions.Count}");
            }
            catch (Exception ex)
            {
                UpdateStatus(
                    "Unable to load historical optimisation result:\n" +
                    ex.Message);
            }
        }


        // =========================================
        // COMPARE SELECTED OPTIMISATION RUNS
        // =========================================

        private void CompareSelectedRuns_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                if (_compareRunACombo.SelectedItem
                    is not OptimisationRunHistoryItem runAItem ||
                    _compareRunBCombo.SelectedItem
                    is not OptimisationRunHistoryItem runBItem)
                {
                    UpdateStatus(
                        "Select two optimisation runs before comparing.");

                    return;
                }

                if (string.Equals(
                    runAItem.RequestId,
                    runBItem.RequestId,
                    StringComparison.Ordinal))
                {
                    UpdateStatus(
                        "Choose two different optimisation runs.");

                    return;
                }

                // Load the saved JSON results. This does not execute Python.
                OptimisationResult runA =
                    _runHistoryService.LoadResult(runAItem);

                OptimisationResult runB =
                    _runHistoryService.LoadResult(runBItem);

                OptimisationRunComparison comparison =
                    _runComparisonService.Compare(runA, runB);

                RenderRunComparison(comparison);

                UpdateStatus(
                    "Optimisation runs compared successfully.\n" +
                    $"Run A: {comparison.RunARequestId}\n" +
                    $"Run B: {comparison.RunBRequestId}");
            }
            catch (Exception ex)
            {
                _runComparisonHost.Children.Clear();
                _runComparisonHost.Children.Add(new TextBlock
                {
                    Text = "Comparison failed: " + ex.Message,
                    FontSize = 10,
                    Foreground = BrushFromHex("#D92D20"),
                    TextWrapping = TextWrapping.Wrap
                });

                UpdateStatus(
                    "Unable to compare optimisation runs:\n" +
                    ex.Message);
            }
        }


        // =========================================
        // RENDER OPTIMISATION RUN COMPARISON
        // =========================================

        private void RenderRunComparison(
            OptimisationRunComparison comparison)
        {
            _runComparisonHost.Children.Clear();

            string objectivesA = FormatObjectives(
                comparison.RunASelectedObjectives);

            string objectivesB = FormatObjectives(
                comparison.RunBSelectedObjectives);

            var summary = new TextBlock
            {
                Text =
                    $"RUN A: {comparison.RunARequestId}\n" +
                    $"RUN B: {comparison.RunBRequestId}\n\n" +
                    $"Selected objectives — A: {objectivesA}\n" +
                    $"Selected objectives — B: {objectivesB}\n\n" +
                    $"Candidates: {comparison.RunACandidateCount}  |  " +
                    $"{comparison.RunBCandidateCount}\n" +
                    $"Pareto solutions: {comparison.RunAParetoCount}  |  " +
                    $"{comparison.RunBParetoCount}\n" +
                    $"Out-of-domain solutions: {comparison.RunAOutOfDomainCount}  |  " +
                    $"{comparison.RunBOutOfDomainCount}\n\n" +
                    "INDEPENDENT OBJECTIVE MINIMA\n" +
                    $"Lowest U-value (W/m²·K): {FormatComparisonMetric(comparison.RunABestUValue)}  |  " +
                    $"{FormatComparisonMetric(comparison.RunBBestUValue)}\n" +
                    $"Lowest carbon (kgCO₂e/m²): {FormatComparisonMetric(comparison.RunABestCarbon)}  |  " +
                    $"{FormatComparisonMetric(comparison.RunBBestCarbon)}\n" +
                    $"Lowest cost (€/m²): {FormatComparisonMetric(comparison.RunABestCost)}  |  " +
                    $"{FormatComparisonMetric(comparison.RunBBestCost)}\n\n" +
                    $"Common alternatives: {comparison.CommonAlternatives.Count}\n" +
                    $"New alternatives in Run B: {comparison.NewAlternatives.Count}\n" +
                    $"Removed from Run B: {comparison.RemovedAlternatives.Count}",
                FontSize = 10,
                Foreground = BrushFromHex("#344054"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            };

            _runComparisonHost.Children.Add(new Border
            {
                Background = Brushes.White,
                BorderBrush = BrushFromHex("#DCE3EA"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10),
                Child = summary
            });

            _runComparisonHost.Children.Add(new TextBlock
            {
                Text = "COMMON ALTERNATIVE METRIC COMPARISON",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = BrushFromHex("#26734D"),
                Margin = new Thickness(0, 12, 0, 6)
            });

            if (comparison.CommonAlternativeDetails.Count == 0)
            {
                _runComparisonHost.Children.Add(new TextBlock
                {
                    Text = "No common material/thickness alternatives.",
                    FontSize = 10,
                    Foreground = BrushFromHex("#667085")
                });
            }
            else
            {
                foreach (var item in comparison.CommonAlternativeDetails.Take(30))
                {
                    var detail = new TextBlock
                    {
                        Text =
                            $"{item.MaterialId} | {item.ThicknessMm:F0} mm\n" +
                            $"U-value: {item.RunAUValue:F3} → {item.RunBUValue:F3} W/(m²·K)\n" +
                            $"Carbon: {FormatComparisonMetric(item.RunACarbon)} → " +
                            $"{FormatComparisonMetric(item.RunBCarbon)} kgCO₂e/m²\n" +
                            $"Cost: {FormatComparisonMetric(item.RunACost)} → " +
                            $"{FormatComparisonMetric(item.RunBCost)} €/m²\n" +
                            $"Out-of-domain: {(item.RunAOutOfDomain ? "YES" : "NO")} → " +
                            $"{(item.RunBOutOfDomain ? "YES" : "NO")}",
                        FontSize = 10,
                        Foreground = BrushFromHex("#475467"),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 8)
                    };

                    _runComparisonHost.Children.Add(new Border
                    {
                        Background = Brushes.White,
                        BorderBrush = BrushFromHex("#E4E7EC"),
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(8),
                        Margin = new Thickness(0, 2, 0, 2),
                        Child = detail
                    });
                }

                if (comparison.CommonAlternativeDetails.Count > 30)
                {
                    _runComparisonHost.Children.Add(new TextBlock
                    {
                        Text = $"Showing 30 of {comparison.CommonAlternativeDetails.Count} common alternatives.",
                        FontSize = 9,
                        Foreground = BrushFromHex("#667085")
                    });
                }
            }

            AddComparisonList(
                "NEW IN RUN B",
                comparison.NewAlternatives,
                "#2685A6");

            AddComparisonList(
                "REMOVED FROM RUN A",
                comparison.RemovedAlternatives,
                "#9A5B13");

            _runComparisonHost.Children.Add(new TextBlock
            {
                Text =
                    "Interpretation: each row compares the same material and thickness " +
                    "across the two saved Pareto sets. Objective minima above are " +
                    "independent values and may come from different solutions. " +
                    "Carbon and cost remain illustrative; out-of-domain predictions " +
                    "require verification.",
                FontSize = 10,
                Foreground = BrushFromHex("#9A5B13"),
                Background = BrushFromHex("#FFF4E5"),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 8, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
        }

        private static string FormatObjectives(
            System.Collections.Generic.IReadOnlyCollection<string> objectives)
        {
            if (objectives == null || objectives.Count == 0)
                return "Not recorded in saved run";

            return string.Join(", ", objectives.Select(objective =>
            {
                return objective switch
                {
                    "minimise_u_value" => "U-value",
                    "minimise_carbon" => "Embodied carbon",
                    "minimise_cost" => "Material cost",
                    _ => objective
                };
            }));
        }

        private void AddComparisonList(
            string heading,
            System.Collections.Generic.IReadOnlyCollection<string> items,
            string headingColour)
        {
            var panel = new StackPanel
            {
                Margin = new Thickness(0, 8, 0, 0)
            };

            panel.Children.Add(new TextBlock
            {
                Text = $"{heading} ({items.Count})",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = BrushFromHex(headingColour),
                Margin = new Thickness(0, 0, 0, 4)
            });

            string content = items.Count == 0
                ? "None"
                : string.Join("\n", items.Take(12));

            if (items.Count > 12)
                content += $"\n...and {items.Count - 12} more.";

            panel.Children.Add(new TextBlock
            {
                Text = content,
                FontSize = 10,
                Foreground = BrushFromHex("#475467"),
                TextWrapping = TextWrapping.Wrap
            });

            _runComparisonHost.Children.Add(panel);
        }


        private static string FormatComparisonMetric(double? value)
        {
            return value.HasValue
                ? value.Value.ToString("F3")
                : "N/A";
        }


        // =========================================
        // LOAD PROJECT AND CLASSIFY
        // =========================================

        private void ProjectButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            try
            {
                if (!ProjectAnalysisEventManager.IsInitialized)
                {
                    UpdateStatus(
                        "Project analysis is not initialized. " +
                        "Close and reopen the Performance Dashboard.");

                    return;
                }

                UpdateStatus(
                    "Project analysis requested. " +
                    "Collecting walls from the active Revit model...");

                if (sender is Button button)
                    button.IsEnabled = false;

                ProjectAnalysisEventManager.RequestAnalysis(result =>
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            if (!result.Success)
                            {
                                UpdateStatus(
                                    "Project analysis failed:\n" +
                                    result.Message);

                                return;
                            }

                            // 1. Import the JSON generated by Revit.
                            var project = _importService.ImportProject(
                                result.JsonFilePath);

                            // 2. Update the existing dashboard ViewModel.
                            _viewModel.LoadProjectData(project);

                            _projectOverviewExpander.IsExpanded = true;

                            // 3. Read classification persisted in JSON.
                            int goodCount =
                                CountClassifiedInstances(project.WallTypes, "GOOD");

                            int moderateCount =
                                CountClassifiedInstances(project.WallTypes, "MODERATE");

                            int poorCount =
                                CountClassifiedInstances(project.WallTypes, "POOR");

                            int notAnalysedCount =
                                CountClassifiedInstances(project.WallTypes, "NOT ANALYSED");

                            int unsupportedCount =
                                CountClassifiedInstances(project.WallTypes, "UNSUPPORTED");

                            // 4. Calculate instance-weighted average U-value.
                            var validRecords = project.WallTypes
                                .Where(w =>
                                    w.ThermalPerformance?.IsValid == true &&
                                    w.ThermalPerformance.UValue.HasValue &&
                                    double.IsFinite(
                                        w.ThermalPerformance.UValue.Value) &&
                                    w.ThermalPerformance.UValue.Value > 0 &&
                                    w.InstanceCount > 0)
                                .ToList();

                            string averageUValue = "—";

                            if (validRecords.Count > 0)
                            {
                                double totalWeightedUValue =
                                    validRecords.Sum(w =>
                                        w.ThermalPerformance!.UValue!.Value *
                                        w.InstanceCount);

                                int totalValidInstances =
                                    validRecords.Sum(w => w.InstanceCount);

                                if (totalValidInstances > 0)
                                {
                                    double weightedAverage =
                                        totalWeightedUValue /
                                        totalValidInstances;

                                    averageUValue =
                                        weightedAverage.ToString("F3");
                                }
                            }

                            // 5. Calculate KPI values.
                            //
                            // COMPLIANT = GOOD
                            // NEEDS IMPROVEMENT = POOR
                            //
                            // NOT ANALYSED and UNSUPPORTED remain
                            // separate in the performance distribution.

                            int needsImprovement = poorCount;

                            UpdateKpiValues(
                                project.TotalPlacedWalls.ToString(),
                                averageUValue,
                                goodCount.ToString(),
                                needsImprovement.ToString());

                            // 6. Update five-category performance distribution.
                            UpdateDistribution(
                                goodCount,
                                moderateCount,
                                poorCount,
                                notAnalysedCount,
                                unsupportedCount);

                            // 7. Find best and worst valid assemblies.
                            var validAssemblies = _viewModel.Assemblies
                                .Where(a => a.IsValid)
                                .OrderBy(a => a.UValue)
                                .ToList();

                            string bestAssembly =
                                "No valid assembly available.";

                            string worstAssembly =
                                "No valid assembly available.";

                            if (validAssemblies.Count > 0)
                            {
                                var best = validAssemblies.First();
                                var worst = validAssemblies.Last();

                                bestAssembly =
                                    $"{best.AssemblyName} — " +
                                    $"{best.UValue:F3} W/(m²·K)";

                                worstAssembly =
                                    $"{worst.AssemblyName} — " +
                                    $"{worst.UValue:F3} W/(m²·K)";
                            }

                            // 8. Update project insights.
                            string dataQuality =
                                $"Successful extractions: {project.SuccessfulExtractions}\n" +
                                $"Failed extractions: {project.FailedExtractions}\n" +
                                $"Invalid calculations: {project.InvalidAssemblies}\n" +
                                $"Unsupported wall functions: {unsupportedCount}\n" +
                                $"Not analysed: {notAnalysedCount}";

                            UpdateInsights(
                                bestAssembly,
                                worstAssembly,
                                $"{_viewModel.BestImprovement:F1}% improvement compared with baseline.",
                                dataQuality);

                            // 9. Update system status.
                            UpdateStatus(
                                $"Project analysis completed.\n" +
                                $"Project: {project.ProjectName}\n" +
                                $"Placed walls: {project.TotalPlacedWalls}\n" +
                                $"Unique wall types: {project.UniqueWallTypes}\n\n" +
                                $"GOOD: {goodCount}\n" +
                                $"MODERATE: {moderateCount}\n" +
                                $"POOR: {poorCount}\n" +
                                $"NOT ANALYSED: {notAnalysedCount}\n" +
                                $"UNSUPPORTED: {unsupportedCount}\n\n" +
                                $"Export: {result.JsonFilePath}");
                        }
                        catch (Exception ex)
                        {
                            UpdateStatus(
                                "Dashboard update failed:\n" + ex.Message);
                        }
                        finally
                        {
                            if (sender is Button completedButton)
                                completedButton.IsEnabled = true;
                        }
                    }));
                });
            }
            catch (Exception ex)
            {
                if (sender is Button button)
                    button.IsEnabled = true;

                UpdateStatus(
                    "Unable to start project analysis:\n" + ex.Message);
            }
        }

        // =========================================
        // INSPECT SELECTED REVIT WALL
        // =========================================

        private void SelectionButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                if (!PerformanceSelectionEventManager.IsInitialized)
                {
                    UpdateStatus(
                        "Selection service is not initialized. " +
                        "Close and reopen the Performance Dashboard.");

                    return;
                }

                UpdateStatus(
                    "Selection request sent to Revit. Select a wall in the model.");

                PerformanceSelectionEventManager.SetCompletionCallback(
                    result =>
                    {
                        Dispatcher.BeginInvoke(
                            new Action(() => DisplaySelectionResult(result)));
                    });

                PerformanceSelectionEventManager.Raise();
            }
            catch (Exception ex)
            {
                UpdateStatus(
                    "Unable to start wall selection.\n" + ex.Message);
            }
        }

        // =========================================
        // DISPLAY EXTRACTED WALL INFORMATION
        // =========================================

        private void DisplaySelectionResult(
            PerformanceWallSelectionResult result)
        {
            if (result.WasCancelled)
            {
                UpdateStatus("Wall selection cancelled.");
                return;
            }

            if (!result.Success)
            {
                UpdateStatus(
                    "Wall extraction failed.\n" + result.Message);

                return;
            }

            if (result.Assembly == null)
            {
                UpdateStatus(
                    "Wall was selected, but no thermal assembly was returned.");

                return;
            }

            _selectedWallNameText.Text =
                $"{result.WallName} | {result.WallTypeName}";

            _selectedWallLayersPanel.Children.Clear();
            _selectedWallDiagnosticsPanel.Children.Clear();
            _selectedWallQualityPanel.Children.Clear();

            var calculator = new ThermalCalculator();

            ThermalCalculationResult calculation =
                calculator.CalculateAssembly(
                    result.Assembly.Layers,
                    HeatFlowDirection.Horizontal);

            if (!calculation.IsValid)
            {
                _selectedWallUValueText.Text =
                    "U-value: Cannot verify";

                _selectedWallResistanceText.Text =
                    "Total resistance: Not calculated";

                _selectedWallUValueText.Foreground =
                    BrushFromHex("#D99A32");

                _selectedWallLayersPanel.Children.Add(new TextBlock
                {
                    Text = "Calculation issues:",
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = BrushFromHex("#D9534F"),
                    Margin = new Thickness(0, 0, 0, 5)
                });

                foreach (string error in calculation.Errors)
                {
                    _selectedWallLayersPanel.Children.Add(new TextBlock
                    {
                        Text = "• " + error,
                        FontSize = 11,
                        Foreground = BrushFromHex("#667085"),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 4)
                    });
                }

                var invalidReport =
                    new AssemblyDiagnosticReportService().Generate(
                        result.Assembly,
                        calculation);

                RenderAssemblyDiagnosticReport(invalidReport);
                RenderMaterialQuality(invalidReport.MaterialDataQuality);

                UpdateStatus(
                    $"Wall extracted: {result.WallName}. " +
                    "Thermal calculation could not be verified.");

                return;
            }

            _selectedWallUValueText.Text =
                $"U-value: {calculation.UValue.Value:F3} W/(m²·K)";

            _selectedWallUValueText.Foreground =
                BrushFromHex("#2685A6");

            _selectedWallResistanceText.Text =
                $"Total resistance: {calculation.TotalResistance:F3} m²·K/W  |  " +
                $"Layer resistance: {calculation.LayerResistance:F3} m²·K/W";

            for (int index = 0;
                 index < result.Assembly.Layers.Count;
                 index++)
            {
                var layer = result.Assembly.Layers[index];
                var layerResult = calculation.LayerResults[index];

                var layerCard = new Border
                {
                    Background = BrushFromHex("#F7F9FB"),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(9),
                    Margin = new Thickness(0, 0, 0, 7)
                };

                var layerContent = new StackPanel();

                layerContent.Children.Add(new TextBlock
                {
                    Text = $"{index + 1}. {layer.LayerName}",
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = BrushFromHex("#172B4D"),
                    TextWrapping = TextWrapping.Wrap
                });

                string conductivityDisplay =
                    layer.ThermalConductivityWmK.HasValue
                        ? layer.ThermalConductivityWmK.Value.ToString("F4")
                        : "Unknown";

                layerContent.Children.Add(new TextBlock
                {
                    Text =
                        $"Thickness: {layer.ThicknessMm:F1} mm  |  " +
                        $"Conductivity: {conductivityDisplay} W/(m·K)",
                    FontSize = 10,
                    Foreground = BrushFromHex("#667085"),
                    Margin = new Thickness(0, 4, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                });

                layerContent.Children.Add(new TextBlock
                {
                    Text =
                        $"Resistance: {layerResult.ThermalResistance:F3} m²·K/W  |  " +
                        $"Contribution: {layerResult.ResistanceContributionPercentage:F1}%",
                    FontSize = 10,
                    Foreground = BrushFromHex("#2685A6"),
                    Margin = new Thickness(0, 3, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                });

                layerContent.Children.Add(new TextBlock
                {
                    Text = $"Conductivity source: {layer.ConductivitySource}",
                    FontSize = 9,
                    Foreground = BrushFromHex("#98A2B3"),
                    Margin = new Thickness(0, 3, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                });

                layerCard.Child = layerContent;

                _selectedWallLayersPanel.Children.Add(layerCard);
            }

            // Surface resistances
            _selectedWallLayersPanel.Children.Add(new TextBlock
            {
                Text =
                    $"Surface resistances: Internal " +
                    $"{calculation.InternalSurfaceResistance:F2} + External " +
                    $"{calculation.ExternalSurfaceResistance:F2} m²·K/W",
                FontSize = 10,
                Foreground = BrushFromHex("#667085"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0)
            });

            var assemblyReport =
                new AssemblyDiagnosticReportService().Generate(
                    result.Assembly,
                    calculation);

            RenderAssemblyDiagnosticReport(assemblyReport);
            RenderMaterialQuality(assemblyReport.MaterialDataQuality);

            UpdateStatus(
                $"Thermal calculation completed for {result.WallName}. " +
                $"U-value: {calculation.UValue.Value:F3} W/(m²·K).");
        }

        // =========================================
        // CLASSIFICATION COUNTS FROM IMPORTED JSON
        // =========================================

        private static int CountClassifiedInstances(
            System.Collections.Generic.IEnumerable<ThermalEngine.UI.Models.ProjectWallRecord> records,
            string status)
        {
            return records
                .Where(record => string.Equals(
                    record.ThermalClassificationStatus?.Trim(),
                    status,
                    StringComparison.OrdinalIgnoreCase))
                .Sum(record => Math.Max(0, record.InstanceCount));
        }

        // =========================================
        // SELECTED WALL TAB HELPERS
        // =========================================

        private static ScrollViewer CreateTabContent(UIElement content)
        {
            return new ScrollViewer
            {
                Content = content,
                MaxHeight = 460,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
        }

        private static void AddEmptyState(StackPanel panel, string message)
        {
            panel.Children.Add(new TextBlock
            {
                Text = message,
                FontSize = 11,
                Foreground = BrushFromHex("#98A2B3"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(4, 4, 4, 8)
            });
        }

        private static void AddFinding(
            StackPanel panel,
            string message,
            string colour = "#667085")
        {
            panel.Children.Add(new TextBlock
            {
                Text = "• " + message,
                FontSize = 11,
                Foreground = BrushFromHex(colour),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(4, 2, 4, 6)
            });
        }

        private static void AddSectionHeading(StackPanel panel, string title)
        {
            panel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = BrushFromHex("#475467"),
                Margin = new Thickness(4, 12, 4, 5)
            });
        }

        private void RenderLayerDiagnostics(LayerDiagnosticResult diagnostic)
        {
            _selectedWallDiagnosticsPanel.Children.Clear();

            if (diagnostic.DominantLayer != null)
            {
                var dominant = diagnostic.DominantLayer;
                var card = new Border
                {
                    Background = BrushFromHex("#EAF4F8"),
                    CornerRadius = new CornerRadius(7),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var stack = new StackPanel();
                stack.Children.Add(new TextBlock
                {
                    Text = "DOMINANT RESISTANCE LAYER",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = BrushFromHex("#2685A6")
                });
                stack.Children.Add(new TextBlock
                {
                    Text = dominant.LayerName,
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = BrushFromHex("#172B4D"),
                    Margin = new Thickness(0, 5, 0, 3)
                });
                stack.Children.Add(new TextBlock
                {
                    Text = $"{dominant.ResistanceContributionPercentage:F1}% of total assembly resistance",
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = BrushFromHex("#2685A6")
                });
                stack.Children.Add(new TextBlock
                {
                    Text = dominant.Explanation,
                    FontSize = 10,
                    Foreground = BrushFromHex("#667085"),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 5, 0, 0)
                });

                card.Child = stack;
                _selectedWallDiagnosticsPanel.Children.Add(card);
            }

            AddSectionHeading(_selectedWallDiagnosticsPanel, "SUMMARY");
            AddFinding(_selectedWallDiagnosticsPanel, diagnostic.Summary, "#172B4D");

            AddSectionHeading(_selectedWallDiagnosticsPanel, "KEY FINDINGS");
            foreach (string finding in diagnostic.Findings)
                AddFinding(_selectedWallDiagnosticsPanel, finding);

            AddSectionHeading(_selectedWallDiagnosticsPanel, "LAYER CONTRIBUTIONS");

            foreach (var layer in diagnostic.Layers)
            {
                var row = new Grid { Margin = new Thickness(4, 4, 4, 2) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var name = new TextBlock
                {
                    Text = layer.LayerName,
                    FontSize = 10,
                    Foreground = BrushFromHex("#475467"),
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var percentage = new TextBlock
                {
                    Text = $"{layer.ResistanceContributionPercentage:F1}%",
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = BrushFromHex("#172B4D"),
                    Margin = new Thickness(8, 0, 0, 0)
                };

                Grid.SetColumn(name, 0);
                Grid.SetColumn(percentage, 1);
                row.Children.Add(name);
                row.Children.Add(percentage);
                _selectedWallDiagnosticsPanel.Children.Add(row);

                _selectedWallDiagnosticsPanel.Children.Add(new Border
                {
                    Height = 7,
                    Background = BrushFromHex("#EDF0F4"),
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(4, 2, 4, 8),
                    Child = new Border
                    {
                        Height = 7,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Width = 260 * Math.Max(0, Math.Min(100, layer.ResistanceContributionPercentage)) / 100.0,
                        Background = BrushFromHex(layer.ContributionCategory == "DOMINANT" ? "#2685A6" : "#98A2B3"),
                        CornerRadius = new CornerRadius(4)
                    }
                });
            }
        }

        private void RenderAssemblyDiagnosticReport(
            AssemblyDiagnosticReport report)
        {
            // Preserve the detailed layer contribution visualization.
            RenderLayerDiagnostics(report.LayerDiagnostics);

            // Add a concise architect-facing summary at the top of Diagnostics.
            var summaryCard = new Border
            {
                Background = BrushFromHex("#F0F5FA"),
                BorderBrush = BrushFromHex("#D8E4EF"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var summaryStack = new StackPanel();

            summaryStack.Children.Add(new TextBlock
            {
                Text = "ARCHITECT SUMMARY",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = BrushFromHex("#2685A6")
            });

            summaryStack.Children.Add(new TextBlock
            {
                Text = report.ArchitectSummary,
                FontSize = 11,
                Foreground = BrushFromHex("#172B4D"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 0)
            });

            var statusGrid = new Grid
            {
                Margin = new Thickness(0, 9, 0, 0)
            };
            statusGrid.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            statusGrid.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var calculationStatus = new TextBlock
            {
                Text = $"CALCULATION: {report.CalculationStatus}",
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = BrushFromHex(
                    report.CalculationIsValid ? "#239B75" : "#D9534F")
            };

            var inputStatus = new TextBlock
            {
                Text = $"INPUT DATA: {report.InputDataStatus}",
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = BrushFromHex(
                    report.InputDataRequiresReview ? "#D99A32" : "#239B75"),
                TextWrapping = TextWrapping.Wrap
            };

            Grid.SetColumn(calculationStatus, 0);
            Grid.SetColumn(inputStatus, 1);
            statusGrid.Children.Add(calculationStatus);
            statusGrid.Children.Add(inputStatus);
            summaryStack.Children.Add(statusGrid);

            summaryCard.Child = summaryStack;

            // Put the architect summary first, ahead of technical evidence.
            _selectedWallDiagnosticsPanel.Children.Insert(0, summaryCard);

            AddSectionHeading(_selectedWallDiagnosticsPanel, "RECOMMENDED VERIFICATION");

            foreach (string action in report.VerificationActions)
            {
                AddFinding(_selectedWallDiagnosticsPanel, action, "#8A641D");
            }

            AddSectionHeading(_selectedWallDiagnosticsPanel, "DIAGNOSTIC OBSERVATIONS");

            foreach (string observation in report.Observations)
            {
                AddFinding(_selectedWallDiagnosticsPanel, observation);
            }
        }

        private void RenderMaterialQuality(MaterialDataQualityResult quality)
        {
            _selectedWallQualityPanel.Children.Clear();

            string statusColour = quality.Status switch
            {
                "COMPLETE" => "#239B75",
                "REQUIRES REVIEW" => "#D99A32",
                "INVALID DATA" => "#D9534F",
                _ => "#667085"
            };

            var statusCard = new Border
            {
                Background = BrushFromHex("#F5F7FA"),
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var statusStack = new StackPanel();
            statusStack.Children.Add(new TextBlock
            {
                Text = quality.Status,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = BrushFromHex(statusColour)
            });
            statusStack.Children.Add(new TextBlock
            {
                Text = $"{quality.LayersRequiringReview} of {quality.TotalLayers} layers require review.",
                FontSize = 11,
                Foreground = BrushFromHex("#667085"),
                Margin = new Thickness(0, 4, 0, 0)
            });
            statusCard.Child = statusStack;
            _selectedWallQualityPanel.Children.Add(statusCard);

            AddSectionHeading(_selectedWallQualityPanel, "DATA SUMMARY");
            AddFinding(_selectedWallQualityPanel, $"Verified manufacturer data: {quality.VerifiedLayers}");
            AddFinding(_selectedWallQualityPanel, $"Revit thermal asset data: {quality.RevitAssetLayers}");
            AddFinding(_selectedWallQualityPanel, $"Unknown sources: {quality.UnknownSourceLayers}");
            AddFinding(_selectedWallQualityPanel, $"Missing conductivity: {quality.MissingConductivityLayers}");
            AddFinding(_selectedWallQualityPanel, $"Invalid conductivity: {quality.InvalidConductivityLayers}");
            AddFinding(_selectedWallQualityPanel, $"Invalid thickness: {quality.InvalidThicknessLayers}");
            AddFinding(_selectedWallQualityPanel, $"Missing material names: {quality.MissingNameLayers}");

            AddSectionHeading(_selectedWallQualityPanel, "MATERIAL REVIEW");

            foreach (var layer in quality.Layers)
            {
                string colour = layer.RequiresReview ? "#D99A32" : "#239B75";
                var card = new Border
                {
                    Background = Brushes.White,
                    BorderBrush = BrushFromHex("#E4E9EF"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(9),
                    Margin = new Thickness(0, 0, 0, 7)
                };

                var stack = new StackPanel();
                stack.Children.Add(new TextBlock
                {
                    Text = layer.LayerName,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = BrushFromHex("#172B4D"),
                    TextWrapping = TextWrapping.Wrap
                });
                stack.Children.Add(new TextBlock
                {
                    Text = layer.DataStatus,
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = BrushFromHex(colour),
                    Margin = new Thickness(0, 3, 0, 0)
                });
                stack.Children.Add(new TextBlock
                {
                    Text = $"Thickness: {layer.ThicknessStatus} | Conductivity: {layer.ConductivityStatus}",
                    FontSize = 10,
                    Foreground = BrushFromHex("#667085"),
                    Margin = new Thickness(0, 3, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                });
                stack.Children.Add(new TextBlock
                {
                    Text = layer.SourceDescription,
                    FontSize = 10,
                    Foreground = BrushFromHex("#667085"),
                    Margin = new Thickness(0, 2, 0, 0)
                });

                foreach (string finding in layer.Findings)
                    AddFinding(stack, finding);

                card.Child = stack;
                _selectedWallQualityPanel.Children.Add(card);
            }

            AddSectionHeading(_selectedWallQualityPanel, "QUALITY FINDINGS");
            foreach (string finding in quality.Findings)
                AddFinding(_selectedWallQualityPanel, finding);
        }

        // =========================================
        // KPI CARD BUILDER
        // =========================================

        private Border CreateKpiCard(
            string title,
            string value,
            string description,
            string accentColour,
            out TextBlock valueText)
        {
            var card = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(8),
                BorderBrush = BrushFromHex("#DCE3EA"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var layout = new StackPanel();

            layout.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = BrushFromHex("#667085")
            });

            valueText = new TextBlock
            {
                Text = value,
                FontSize = 25,
                FontWeight = FontWeights.Bold,
                Foreground = BrushFromHex(accentColour),
                Margin = new Thickness(0, 8, 0, 4)
            };

            layout.Children.Add(valueText);

            layout.Children.Add(new TextBlock
            {
                Text = description,
                FontSize = 10,
                Foreground = BrushFromHex("#98A2B3")
            });

            card.Child = layout;

            return card;
        }

        // =========================================
        // DISTRIBUTION ROW BUILDER
        // =========================================

        private StackPanel CreateDistributionRow(
            string category,
            string colour,
            out TextBlock countText,
            out Border progressBar)
        {
            var row = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 16)
            };

            var labelGrid = new Grid();

            labelGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(
                        1,
                        GridUnitType.Star)
                });

            labelGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = GridLength.Auto
                });

            var label = new TextBlock
            {
                Text = category,
                FontSize = 11,
                Foreground = BrushFromHex("#475467")
            };

            countText = new TextBlock
            {
                Text = "—",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = BrushFromHex("#172B4D")
            };

            Grid.SetColumn(label, 0);
            Grid.SetColumn(countText, 1);

            labelGrid.Children.Add(label);
            labelGrid.Children.Add(countText);

            row.Children.Add(labelGrid);

            var barBackground = new Border
            {
                Height = 8,
                Background = BrushFromHex("#EDF0F4"),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 8, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            progressBar = new Border
            {
                Width = 0,
                Height = 8,
                Background = BrushFromHex(colour),
                CornerRadius = new CornerRadius(4),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            barBackground.Child = progressBar;

            row.Children.Add(barBackground);

            return row;
        }

        // =========================================
        // INSIGHT CARD BUILDER
        // =========================================

        private Border CreateInsightCard(
            string title,
            string description,
            string accentColour,
            out TextBlock descriptionText)
        {
            var card = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(8),
                BorderBrush = BrushFromHex("#DCE3EA"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var layout = new StackPanel();

            layout.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = BrushFromHex(accentColour)
            });

            descriptionText = new TextBlock
            {
                Text = description,
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Foreground = BrushFromHex("#667085"),
                Margin = new Thickness(0, 8, 0, 0)
            };

            layout.Children.Add(descriptionText);

            card.Child = layout;

            return card;
        }

        // =========================================
        // UPDATE KPI VALUES
        // =========================================

        public void UpdateKpiValues(
            string totalWalls,
            string averageUValue,
            string compliant,
            string needsReview)
        {
            _totalWallsValue.Text = totalWalls;
            _averageUValueValue.Text = averageUValue;
            _compliantValue.Text = compliant;
            _needsReviewValue.Text = needsReview;
        }

        // =========================================
        // UPDATE PERFORMANCE DISTRIBUTION
        // =========================================

        public void UpdateDistribution(
            int good,
            int moderate,
            int poor,
            int notAnalysed,
            int unsupported)
        {
            if (good < 0 || moderate < 0 ||
                poor < 0 || notAnalysed < 0 ||
                unsupported < 0)
            {
                return;
            }

            int total =
                good + moderate + poor + notAnalysed + unsupported;

            _goodCount.Text = good.ToString();
            _moderateCount.Text = moderate.ToString();
            _poorCount.Text = poor.ToString();
            _notAnalysedCount.Text = notAnalysed.ToString();
            _unsupportedCount.Text = unsupported.ToString();

            if (total == 0)
            {
                _goodBar.Width = 0;
                _moderateBar.Width = 0;
                _poorBar.Width = 0;
                _notAnalysedBar.Width = 0;
                _unsupportedBar.Width = 0;

                return;
            }

            _goodBar.Width =
                DistributionBarWidth * good / total;

            _moderateBar.Width =
                DistributionBarWidth * moderate / total;

            _poorBar.Width =
                DistributionBarWidth * poor / total;

            _notAnalysedBar.Width =
                DistributionBarWidth * notAnalysed / total;

            _unsupportedBar.Width =
                DistributionBarWidth * unsupported / total;
        }

        // =========================================
        // UPDATE PROJECT INSIGHTS
        // =========================================

        public void UpdateInsights(
            string bestAssembly,
            string worstAssembly,
            string improvement,
            string dataQuality)
        {
            _bestAssemblyText.Text = bestAssembly;
            _worstAssemblyText.Text = worstAssembly;
            _improvementText.Text = improvement;
            _dataQualityText.Text = dataQuality;
        }

        // =========================================
        // UPDATE SYSTEM STATUS
        // =========================================

        public void UpdateStatus(string message)
        {
            // Startup callbacks may arrive before the status control is ready.
            if (_statusText == null)
                return;

            _statusText.Text = message;
        }
    }
}