using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

using SpecSynthAI.AssemblyOptimisation.Models.MultiObjective.Integration;

namespace SpecSynthAI.UI
{
    /// <summary>
    /// Displays the returned Pareto solutions and their research limitations.
    /// </summary>
    public sealed class OptimisationResultsView : UserControl
    {
        private static Brush BrushFromHex(string hex)
        {
            return new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(hex)!);
        }

        public OptimisationResultsView(OptimisationResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            var root = new StackPanel
            {
                Background = Brushes.White,
                Margin = new Thickness(0, 8, 0, 8)
            };

            root.Children.Add(new TextBlock
            {
                Text = "PARETO OPTIMISATION RESULTS",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = BrushFromHex("#172B4D"),
                Margin = new Thickness(0, 0, 0, 8)
            });

            root.Children.Add(new TextBlock
            {
                Text = $"Request: {result.RequestId}\n" +
                       $"Candidates: {result.Summary.CandidateCount}  |  " +
                       $"Pareto solutions: {result.Summary.ParetoSolutionCount}\n" +
                       $"Exhaustive reference Pareto count: {result.Summary.ExhaustiveReferenceParetoCount}\n" +
                       $"Matches exhaustive reference: {(result.Summary.MatchesExhaustiveReference ? "YES" : "NO / NOT CONFIRMED")}",
                FontSize = 11,
                Foreground = BrushFromHex("#2685A6"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var provenance = result.Provenance;
            root.Children.Add(new TextBlock
            {
                Text = $"Model: {provenance?.Model ?? "Not specified"}\n" +
                       $"Algorithm: {provenance?.OptimisationAlgorithm ?? "Not specified"}\n" +
                       $"Training data: {provenance?.TrainingDataScope ?? "Not specified"}\n" +
                       $"Carbon/cost data: {provenance?.CarbonCostDataStatus ?? "Not specified"}\n" +
                       $"Execution mode: {provenance?.ExecutionMode ?? "Not specified"}\n" +
                       $"Python engine invoked: {(provenance?.EngineInvoked == true ? "YES" : "NOT CONFIRMED")}",
                FontSize = 10,
                Foreground = BrushFromHex("#667085"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12)
            });

            var grid = new DataGrid
            {
                ItemsSource = result.Solutions,
                AutoGenerateColumns = false,
                IsReadOnly = true,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                CanUserReorderColumns = true,
                CanUserResizeColumns = true,
                SelectionMode = DataGridSelectionMode.Single,
                SelectionUnit = DataGridSelectionUnit.FullRow,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                Background = Brushes.White,
                RowBackground = Brushes.White,
                AlternatingRowBackground = BrushFromHex("#F4F6F8"),
                BorderBrush = BrushFromHex("#DCE3EA"),
                BorderThickness = new Thickness(1),
                MaxHeight = 420,
                MinHeight = 180
            };

            AddColumn(grid, "Material", "MaterialLabel", 150);
            AddColumn(grid, "Material ID", "MaterialId", 125);
            AddColumn(grid, "Thickness (mm)", "ThicknessMm", 95, "F1");
            AddColumn(grid, "U-value", "UValueWm2K", 75, "F3");
            AddColumn(grid, "Carbon kgCO₂e/m²", "CarbonKgCo2eM2", 105, "F2");
            AddColumn(grid, "Cost €/m²", "CostEurM2", 85, "F2");
            AddColumn(grid, "Wall (mm)", "WallThicknessMm", 80, "F1");
            AddColumn(grid, "Feasible", "Feasible", 65);
            AddColumn(grid, "Out of domain", "ModelOutOfDomain", 95);

            root.Children.Add(grid);

            var outOfDomain = result.Solutions
                .Where(solution => solution.ModelOutOfDomain)
                .ToList();

            var warningText =
                outOfDomain.Count == 0
                    ? "No returned Pareto solution is flagged as out-of-domain."
                    : "OUT-OF-DOMAIN PARETO SOLUTIONS:\n" +
                      string.Join("\n", outOfDomain.Select(solution =>
                          $"{solution.MaterialLabel} ({solution.MaterialId}): " +
                          (string.IsNullOrWhiteSpace(solution.OutOfDomainFeatures)
                              ? "feature not specified"
                              : solution.OutOfDomainFeatures)));

            root.Children.Add(new TextBlock
            {
                Text = warningText,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 10,
                Foreground = BrushFromHex(outOfDomain.Count > 0 ? "#9A5B13" : "#26734D"),
                Background = BrushFromHex(outOfDomain.Count > 0 ? "#FFF4E5" : "#EAF7F0"),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 12, 0, 0)
            });

            root.Children.Add(new TextBlock
            {
                Text = "RESEARCH LIMITATIONS: The optimisation uses a controlled Stage 7.2 demonstration request, not the currently selected Revit wall assembly. The model training data is synthetic. Carbon and cost values are illustrative. Out-of-domain predictions require additional verification. These results are decision-support outputs, not verified product recommendations.",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 10,
                Foreground = BrushFromHex("#9A5B13"),
                Background = BrushFromHex("#FFF4E5"),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 8, 0, 0)
            });

            Content = root;
        }

        private static void AddColumn(
            DataGrid grid,
            string header,
            string propertyName,
            double width,
            string? stringFormat = null)
        {
            var binding = new Binding(propertyName);

            if (!string.IsNullOrWhiteSpace(stringFormat))
                binding.StringFormat = stringFormat;

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = header,
                Binding = binding,
                Width = new DataGridLength(width)
            });
        }
    }
}
