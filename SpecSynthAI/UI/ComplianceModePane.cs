using SpecSynthAI.Models;
using SpecSynthAI.Services;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace SpecSynthAI.UI
{
    public class ComplianceModePane : UserControl
    {
        private readonly TextBlock _modeStatus;
        private readonly TextBlock _recoveryStatus;
        private readonly TextBlock _viewName;
        private readonly TextBlock _summary;
        private readonly TextBlock _passCount;
        private readonly TextBlock _failCount;
        private readonly TextBlock _warningCount;
        private readonly TextBlock _cannotVerifyCount;
        private readonly TextBlock _selectionInfo;
        private readonly TextBlock _inspectionInfo;

        private static readonly Brush BackgroundBrush =
            BrushFromHex("#F4F6F8");

        private static readonly Brush NavyBrush =
            BrushFromHex("#172B4D");

        private static readonly Brush MutedBrush =
            BrushFromHex("#667085");

        public ComplianceModePane()
        {
            MinWidth = 260;
            Background = BackgroundBrush;

            var root = new DockPanel
            {
                LastChildFill = true,
                Background = BackgroundBrush
            };

            // ==========================================
            // HEADER
            // ==========================================

            var header = new Border
            {
                Background = NavyBrush,
                Padding = new Thickness(16, 18, 16, 18)
            };

            var headerStack = new StackPanel();

            headerStack.Children.Add(new TextBlock
            {
                Text = "SPECSYNTH AI",
                Foreground = Brushes.White,
                FontSize = 17,
                FontWeight = FontWeights.Bold
            });

            // COMPLIANCE MODE STATUS
            _modeStatus = new TextBlock
            {
                Text = "COMPLIANCE MODE INACTIVE",
                Foreground = BrushFromHex("#B8C2CC"),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 6, 0, 0)
            };

            headerStack.Children.Add(_modeStatus);

            // RECOVERY STATUS
            _recoveryStatus = new TextBlock
            {
                Text = "NO ACTIVE RECOVERY",
                Foreground = BrushFromHex("#B8C2CC"),
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 5, 0, 0)
            };

            headerStack.Children.Add(_recoveryStatus);

            // ACTIVE VIEW
            _viewName = new TextBlock
            {
                Text = "No heatmap applied",
                Foreground = BrushFromHex("#D6DFEA"),
                FontSize = 11,
                Margin = new Thickness(0, 8, 0, 0)
            };

            headerStack.Children.Add(_viewName);

            header.Child = headerStack;

            DockPanel.SetDock(header, Dock.Top);
            root.Children.Add(header);

            // ==========================================
            // MAIN BODY
            // ==========================================

            var body = new StackPanel
            {
                Margin = new Thickness(14)
            };

            // COMPLIANCE LEGEND
            body.Children.Add(SectionTitle("COMPLIANCE LEGEND"));

            body.Children.Add(LegendItem(
                "PASS",
                "Meets the checked requirements",
                "#2EA05A"));

            body.Children.Add(LegendItem(
                "FAIL",
                "A checked requirement is not met",
                "#DC3545"));

            body.Children.Add(LegendItem(
                "WARNING",
                "Product information is incomplete or unverified",
                "#F5A623"));

            body.Children.Add(LegendItem(
                "CANNOT VERIFY",
                "Insufficient information to determine compliance",
                "#3478BE"));

            body.Children.Add(Divider());

            // MODEL OVERVIEW
            body.Children.Add(SectionTitle("MODEL OVERVIEW"));

            _summary = new TextBlock
            {
                Text = "Apply the compliance heatmap to inspect walls.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = MutedBrush,
                FontSize = 12,
                Margin = new Thickness(0, 8, 0, 8)
            };

            body.Children.Add(_summary);

            _passCount = CountRow("PASS", "#2EA05A");
            _failCount = CountRow("FAIL", "#DC3545");
            _warningCount = CountRow("WARNING", "#F5A623");
            _cannotVerifyCount = CountRow("CANNOT VERIFY", "#3478BE");

            body.Children.Add(_passCount);
            body.Children.Add(_failCount);
            body.Children.Add(_warningCount);
            body.Children.Add(_cannotVerifyCount);

            body.Children.Add(Divider());

            // SELECTED ELEMENT
            body.Children.Add(SectionTitle("SELECTED ELEMENT"));

            _selectionInfo = new TextBlock
            {
                Text = "Select a heatmapped wall.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = MutedBrush,
                FontSize = 12,
                Margin = new Thickness(0, 8, 0, 8)
            };

            body.Children.Add(_selectionInfo);

            body.Children.Add(Divider());

            // INSPECTION RESULTS
            body.Children.Add(SectionTitle("INSPECTION RESULTS"));

            _inspectionInfo = new TextBlock
            {
                Text = "Inspection results will appear here.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = MutedBrush,
                FontSize = 12,
                Margin = new Thickness(0, 8, 0, 8)
            };

            body.Children.Add(_inspectionInfo);

            body.Children.Add(Divider());

            // ZONES
            body.Children.Add(SectionTitle("ZONES"));

            body.Children.Add(new TextBlock
            {
                Text = "Current scope: walls with insulation layers in the active Revit view.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = MutedBrush,
                FontSize = 12,
                Margin = new Thickness(0, 8, 0, 0)
            });

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility =
                    ScrollBarVisibility.Auto,
                Content = body
            };

            root.Children.Add(scroll);

            Content = root;
        }

        // ==========================================
        // UPDATE COMPLIANCE MODE
        // ==========================================

        public void SetModeState(
            bool active,
            int pass,
            int fail,
            int warning,
            int cannotVerify,
            string viewName)
        {
            _modeStatus.Text = active
                ? "COMPLIANCE MODE ACTIVE"
                : "COMPLIANCE MODE INACTIVE";

            _modeStatus.Foreground = active
                ? BrushFromHex("#70E1C1")
                : BrushFromHex("#B8C2CC");

            _viewName.Text = active
                ? "Active view: " + viewName
                : "No heatmap applied";

            _summary.Text = active
                ? $"Walls visualised: {pass + fail + warning + cannotVerify}"
                : "Apply the compliance heatmap to inspect walls.";

            _passCount.Text = $"PASS     {pass}";
            _failCount.Text = $"FAIL     {fail}";
            _warningCount.Text = $"WARNING     {warning}";
            _cannotVerifyCount.Text = $"CANNOT VERIFY     {cannotVerify}";

            if (!active)
            {
                _selectionInfo.Text =
                    "Compliance Mode is inactive.";
            }
        }

        // ==========================================
        // UPDATE RECOVERY STATUS
        // ==========================================

        public void SetRecoveryState(bool hasRecovery)
        {
            _recoveryStatus.Text = hasRecovery
                ? "RECOVERY PROTECTED"
                : "NO ACTIVE RECOVERY";

            _recoveryStatus.Foreground = hasRecovery
                ? BrushFromHex("#70E1C1")
                : BrushFromHex("#B8C2CC");
        }

        // ==========================================
        // UPDATE SELECTED ELEMENT
        // ==========================================

        public void SetSelectionInfo(string message)
        {
            _selectionInfo.Text = message;
        }

        // ==========================================
        // UPDATE INSPECTION RESULTS
        // ==========================================

        public void SetInspectionData(
            WallInspectionData? inspection)
        {
            _inspectionInfo.Inlines.Clear();

            if (inspection == null)
            {
                _inspectionInfo.Text =
                    "Select a heatmapped wall to inspect its properties.";

                return;
            }

            // ------------------------------------------
            // 1. OVERALL WALL COMPLIANCE CARD
            // ------------------------------------------

            // ------------------------------------------
            // 1. OVERALL WALL COMPLIANCE
            // ------------------------------------------

            AppendStatusCard(
                inspection.ComplianceStatusLabel,
                inspection.ComplianceExplanation,
                inspection.ComplianceStatus,
                true);

            // ------------------------------------------
            // 2. WHOLE-WALL THERMAL PERFORMANCE
            // ------------------------------------------

            AppendLine();
            AppendLine("WHOLE-WALL THERMAL PERFORMANCE", true);

            AppendLine(
                $"Calculated U-value: " +
                $"{inspection.WallUValue?.ToString("0.000") ?? "Cannot verify"} W/(m²·K)");

            AppendLine(
                $"Project maximum: " +
                $"{inspection.MaximumWallUValue?.ToString("0.000") ?? "Not available"} W/(m²·K)");

            AppendLine(
                $"Thermal assessment: {inspection.WallThermalStatusLabel}");

            AppendLine(inspection.WallThermalExplanation);

            // ------------------------------------------
            // 3. PRODUCT VERIFICATION
            // ------------------------------------------

            AppendLine();
            AppendLine("PRODUCT VERIFICATION", true);
            AppendLine();

            // ------------------------------------------
            // 2. INDIVIDUAL INSULATION LAYER CARDS
            // ------------------------------------------

            int layerNumber = 1;

            foreach (var layer in inspection.InsulationLayers)
            {
                AppendLayerCard(
                    layerNumber,
                    layer.MaterialName,
                    layer.ComplianceStatusLabel,
                    layer.ComplianceExplanation,
                    layer.ComplianceStatus);

                layerNumber++;
            }

            AppendLine();

            // ------------------------------------------
            // 3. WALL INFORMATION
            // ------------------------------------------

            AppendLine("WALL INFORMATION", true);

            AppendLine($"Element ID: {inspection.ElementId}");
            AppendLine($"Wall: {inspection.WallName}");
            AppendLine($"Wall Type: {inspection.WallTypeName}");

            AppendLine();

            // ------------------------------------------
            // 4. INSPECTION
            // ------------------------------------------

            AppendLine("INSPECTION", true);
            AppendLine(inspection.Message);

            AppendLine();

            // ------------------------------------------
            // 5. PROJECT REQUIREMENT
            // ------------------------------------------

            AppendLine("PROJECT REQUIREMENT", true);

            AppendLine(
                $"Maximum thermal conductivity: " +
                $"{inspection.MaximumThermalConductivity:0.###} " +
                inspection.ThermalUnit);

            // ------------------------------------------
            // 6. DETAILED INSULATION INFORMATION
            // ------------------------------------------

            foreach (var layer in inspection.InsulationLayers)
            {
                AppendLine();
                AppendLine("--------------------------");
                AppendLine("INSULATION LAYER", true);

                AppendLine($"Material: {layer.MaterialName}");

                AppendLine(
                    $"Installed thickness: " +
                    $"{layer.InstalledThicknessMm:0.##} mm");

                // PRODUCT MATCH
                AppendLine();
                AppendLine("PRODUCT MATCH", true);

                AppendLine($"Mapping: {layer.ProductMapping}");
                AppendLine($"Manufacturer: {layer.Manufacturer}");
                AppendLine($"Product: {layer.ProductName}");
                AppendLine($"Product ID: {layer.ProductId}");
                AppendLine($"Verification: {layer.VerificationStatus}");

                // PROPERTY CHECKS
                AppendLine();
                AppendLine("PROPERTY CHECKS", true);

                AppendLine($"Thickness: {layer.ThicknessMatch}");

                AppendLine(
                    $"Thermal conductivity: " +
                    $"{layer.ThermalConductivityWmK?.ToString("0.000") ?? "Not available"}");

                AppendLine($"Thermal result: {layer.ThermalResult}");

                // CATALOGUE INFORMATION
                AppendLine();
                AppendLine("CATALOGUE INFORMATION", true);

                AppendLine(
                    $"Catalogue thickness: " +
                    $"{layer.CatalogueThicknessMm?.ToString("0.##") ?? "Not available"} mm");

                AppendLine(
                    $"Available thicknesses: {layer.AvailableThicknesses}");

                // SOURCE
                AppendLine();
                AppendLine("SOURCE", true);

                AppendLine($"Document: {layer.SourceDocument}");
                AppendLine($"URL: {layer.SourceUrl}");
            }

            AppendLine();
            AppendLine("--------------------------");

            AppendLine(
                "Results are limited to available catalogue data and implemented checks.");
        }

        // ==========================================
        // OVERALL STATUS CARD
        // ==========================================

        private void AppendStatusCard(
            string statusLabel,
            string explanation,
            ComplianceVisualStatus status,
            bool isOverall)
        {
            Brush background = GetStatusBrush(status);
            Brush foreground = Brushes.White;

            var stack = new StackPanel();

            stack.Children.Add(new TextBlock
            {
                Text = statusLabel,
                Foreground = foreground,
                FontSize = isOverall ? 19 : 13,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap
            });

            stack.Children.Add(new TextBlock
            {
                Text = explanation,
                Foreground = foreground,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 7, 0, 0)
            });

            var card = new Border
            {
                Background = background,
                CornerRadius = new CornerRadius(7),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 5, 0, 10),
                Child = stack,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            _inspectionInfo.Inlines.Add(
                new InlineUIContainer(card));

            _inspectionInfo.Inlines.Add(new LineBreak());
        }

        // ==========================================
        // INDIVIDUAL INSULATION LAYER CARD
        // ==========================================

        private void AppendLayerCard(
            int layerNumber,
            string materialName,
            string statusLabel,
            string explanation,
            ComplianceVisualStatus status)
        {
            var stack = new StackPanel();

            // Layer heading
            stack.Children.Add(new TextBlock
            {
                Text = $"INSULATION LAYER {layerNumber:00}",
                Foreground = NavyBrush,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 9)
            });

            // Status badge
            var badgeText = new TextBlock
            {
                Text = statusLabel,
                Foreground = Brushes.White,
                FontSize = 11,
                FontWeight = FontWeights.Bold
            };

            var badge = new Border
            {
                Background = GetStatusBrush(status),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4, 8, 4),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            badge.Child = badgeText;

            stack.Children.Add(badge);

            // Material
            stack.Children.Add(new TextBlock
            {
                Text = materialName,
                Foreground = MutedBrush,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0)
            });

            // Explanation
            stack.Children.Add(new TextBlock
            {
                Text = explanation,
                Foreground = MutedBrush,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 7, 0, 0)
            });

            var card = new Border
            {
                Background = Brushes.White,
                BorderBrush = BrushFromHex("#D9E0E7"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 5, 0, 8),
                Child = stack,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            _inspectionInfo.Inlines.Add(
                new InlineUIContainer(card));

            _inspectionInfo.Inlines.Add(new LineBreak());
        }

        // ==========================================
        // STATUS COLOURS
        // ==========================================

        private static Brush GetStatusBrush(
            ComplianceVisualStatus status)
        {
            return status switch
            {
                ComplianceVisualStatus.Pass =>
                    BrushFromHex("#2EA05A"),

                ComplianceVisualStatus.Fail =>
                    BrushFromHex("#DC3545"),

                ComplianceVisualStatus.Warning =>
                    BrushFromHex("#F5A623"),

                ComplianceVisualStatus.Information =>
                    BrushFromHex("#3478BE"),

                _ => BrushFromHex("#3478BE")
            };
        }

        // ==========================================
        // APPEND TEXT LINE
        // ==========================================

        private void AppendLine(
            string text = "",
            bool bold = false)
        {
            _inspectionInfo.Inlines.Add(
                new Run(text)
                {
                    FontWeight = bold
                        ? FontWeights.Bold
                        : FontWeights.Normal,

                    Foreground = bold
                        ? NavyBrush
                        : MutedBrush
                });

            _inspectionInfo.Inlines.Add(new LineBreak());
        }

        // ==========================================
        // COUNT ROW
        // ==========================================

        private TextBlock CountRow(
            string label,
            string hex)
        {
            return new TextBlock
            {
                Text = label + "     0",
                Foreground = BrushFromHex(hex),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 4, 0, 0)
            };
        }

        // ==========================================
        // DIVIDER
        // ==========================================

        private static UIElement Divider()
        {
            return new Border
            {
                Height = 1,
                Background = BrushFromHex("#D9E0E7"),
                Margin = new Thickness(0, 16, 0, 14)
            };
        }

        // ==========================================
        // SECTION TITLE
        // ==========================================

        private static TextBlock SectionTitle(string text)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = NavyBrush,
                FontSize = 11,
                FontWeight = FontWeights.Bold
            };
        }

        // ==========================================
        // LEGEND ITEM
        // ==========================================

        private static UIElement LegendItem(
            string title,
            string description,
            string hex)
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 12, 0, 0)
            };

            row.Children.Add(new Border
            {
                Width = 14,
                Height = 14,
                CornerRadius = new CornerRadius(3),
                Background = BrushFromHex(hex),
                Margin = new Thickness(0, 2, 10, 0)
            });

            var textStack = new StackPanel();

            textStack.Children.Add(new TextBlock
            {
                Text = title,
                Foreground = NavyBrush,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold
            });

            textStack.Children.Add(new TextBlock
            {
                Text = description,
                Foreground = MutedBrush,
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 230,
                Margin = new Thickness(0, 2, 0, 0)
            });

            row.Children.Add(textStack);

            return row;
        }

        // ==========================================
        // HEX COLOUR BRUSH
        // ==========================================

        private static SolidColorBrush BrushFromHex(string hex)
        {
            var brush = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(hex));

            brush.Freeze();

            return brush;
        }
    }
}