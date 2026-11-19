using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SpecSynthAI.UI
{
    /// <summary>
    /// Data displayed for one insulation layer. This is a presentation model;
    /// it does not change the product catalogue or compliance rules.
    /// </summary>
    public class InspectionLayerResult
    {
        public string MaterialName { get; set; } = "";
        public double InstalledThicknessMm { get; set; }
        public string ProductMapping { get; set; } = "";
        public string ThicknessMatch { get; set; } = "";
        public string ThermalResult { get; set; } = "";
        public string Manufacturer { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string ProductId { get; set; } = "";
        public string VerificationStatus { get; set; } = "";
        public double? CatalogueThicknessMm { get; set; }
        public double? ThermalConductivityWmK { get; set; }
        public string AvailableThicknesses { get; set; } = "";
        public string SourceDocument { get; set; } = "";
        public string SourceUrl { get; set; } = "";
    }

    /// <summary>
    /// Visual inspection results window. Built in C# WPF; no XAML is required.
    /// </summary>
    public class InspectionResultsDialog : Window
    {
        private static readonly Brush Navy = BrushHex("#142B45");
        private static readonly Brush Teal = BrushHex("#087E8B");
        private static readonly Brush Page = BrushHex("#F3F6FA");
        private static readonly Brush Ink = BrushHex("#243447");
        private static readonly Brush Muted = BrushHex("#68788A");
        private static readonly Brush Line = BrushHex("#DCE4ED");
        private static readonly Brush Green = BrushHex("#168451");
        private static readonly Brush Red = BrushHex("#C43D4D");
        private static readonly Brush Amber = BrushHex("#A96B0A");

        public InspectionResultsDialog(
            string wallTypeName,
            int productsLoaded,
            double maximumThermalConductivity,
            IList<InspectionLayerResult> results)
        {
            Title = "SpecSynth AI | Insulation Inspection";
            Width = 900;
            Height = 760;
            MinWidth = 720;
            MinHeight = 560;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = Page;
            FontFamily = new FontFamily("Segoe UI");
            FontSize = 13;
            ShowInTaskbar = false;

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Content = root;

            // Header
            var header = new Border
            {
                Background = Navy,
                Padding = new Thickness(26, 20, 26, 20)
            };

            var headerStack = new StackPanel();
            headerStack.Children.Add(new TextBlock
            {
                Text = "SPECSYNTH AI  /  INSPECTION",
                Foreground = BrushHex("#8ED3D6"),
                FontSize = 11,
                FontWeight = FontWeights.Bold
            });
            headerStack.Children.Add(new TextBlock
            {
                Text = "Insulation Inspection",
                Foreground = Brushes.White,
                FontSize = 25,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 6, 0, 5)
            });
            headerStack.Children.Add(new TextBlock
            {
                Text = "Wall type: " + wallTypeName,
                Foreground = BrushHex("#D6E1EB"),
                FontSize = 13
            });
            headerStack.Children.Add(new TextBlock
            {
                Text = "Products loaded: " + productsLoaded +
                       "     |     Maximum thermal conductivity: " +
                       maximumThermalConductivity.ToString("0.###") + " W/(m·K)",
                Foreground = BrushHex("#D6E1EB"),
                FontSize = 12,
                Margin = new Thickness(0, 5, 0, 0)
            });
            header.Child = headerStack;
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // Scrollable results
            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(24, 20, 24, 18)
            };
            var body = new StackPanel();

            body.Children.Add(new TextBlock
            {
                Text = "INSPECTION RESULTS",
                Foreground = Muted,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 12)
            });

            if (results == null || results.Count == 0)
            {
                body.Children.Add(CreateInfoCard(
                    "No insulation layers were identified",
                    "The selected wall type does not contain a layer assigned the Insulation function."));
            }
            else
            {
                foreach (InspectionLayerResult item in results)
                    body.Children.Add(CreateLayerCard(item));
            }

            body.Children.Add(CreateInfoCard(
                "Important limitation",
                "Material mapping and thickness matching do not prove complete product, " +
                "fire, structural or building-assembly compatibility. Results are limited " +
                "to the available catalogue data and configured project requirement."));

            scroll.Content = body;
            Grid.SetRow(scroll, 1);
            root.Children.Add(scroll);

            // Footer
            var footer = new Border
            {
                Background = Brushes.White,
                BorderBrush = Line,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(24, 12, 24, 12)
            };
            var footerDock = new DockPanel();

            var close = new Button
            {
                Content = "Close",
                MinWidth = 100,
                Padding = new Thickness(18, 9, 18, 9),
                Background = Teal,
                Foreground = Brushes.White,
                BorderBrush = Teal,
                FontWeight = FontWeights.SemiBold,
                IsDefault = true
            };
            close.Click += (_, _) => Close();
            DockPanel.SetDock(close, Dock.Right);
            footerDock.Children.Add(close);

            footerDock.Children.Add(new TextBlock
            {
                Text = "SpecSynth AI  |  Rule-based prototype verification",
                Foreground = Muted,
                VerticalAlignment = VerticalAlignment.Center
            });

            footer.Child = footerDock;
            Grid.SetRow(footer, 2);
            root.Children.Add(footer);
        }

        private static UIElement CreateLayerCard(InspectionLayerResult item)
        {
            var card = new Border
            {
                Background = Brushes.White,
                BorderBrush = Line,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(18),
                Margin = new Thickness(0, 0, 0, 14)
            };

            var stack = new StackPanel();

            var titleRow = new DockPanel { LastChildFill = true };
            var thermalBadge = CreateBadge(item.ThermalResult);
            DockPanel.SetDock(thermalBadge, Dock.Right);
            titleRow.Children.Add(thermalBadge);
            titleRow.Children.Add(new TextBlock
            {
                Text = item.MaterialName,
                FontSize = 17,
                FontWeight = FontWeights.SemiBold,
                Foreground = Ink,
                VerticalAlignment = VerticalAlignment.Center
            });
            stack.Children.Add(titleRow);

            stack.Children.Add(new TextBlock
            {
                Text = "Installed thickness: " + item.InstalledThicknessMm.ToString("0.0") + " mm",
                Foreground = Muted,
                Margin = new Thickness(0, 5, 0, 13)
            });

            stack.Children.Add(SectionLabel("PRODUCT MATCH"));
            stack.Children.Add(PropertyRow("Product mapping", item.ProductMapping));
            stack.Children.Add(PropertyRow("Manufacturer", item.Manufacturer));
            stack.Children.Add(PropertyRow("Product", item.ProductName));
            stack.Children.Add(PropertyRow("Product ID", item.ProductId));
            stack.Children.Add(PropertyRow("Verification status", item.VerificationStatus));

            stack.Children.Add(SectionLabel("PROPERTY CHECKS"));
            stack.Children.Add(ResultRow(
                "Thickness match",
                item.ThicknessMatch,
                item.ThicknessMatch == "EXACT MATCH" ? Green :
                    item.ThicknessMatch == "NO EXACT MATCH" ? Red : Amber));

            stack.Children.Add(PropertyRow(
                "Catalogue thickness",
                item.CatalogueThicknessMm.HasValue
                    ? item.CatalogueThicknessMm.Value.ToString("0.0") + " mm"
                    : "Not available"));

            stack.Children.Add(PropertyRow(
                "Available thicknesses",
                item.AvailableThicknesses));

            stack.Children.Add(PropertyRow(
                "Thermal conductivity",
                item.ThermalConductivityWmK.HasValue
                    ? item.ThermalConductivityWmK.Value.ToString("0.000") + " W/(m·K)"
                    : "Not available"));

            stack.Children.Add(ResultRow(
                "Thermal requirement",
                item.ThermalResult,
                StatusBrush(item.ThermalResult)));

            if (!string.IsNullOrWhiteSpace(item.SourceDocument) ||
                !string.IsNullOrWhiteSpace(item.SourceUrl))
            {
                stack.Children.Add(SectionLabel("SOURCE"));
                if (!string.IsNullOrWhiteSpace(item.SourceDocument))
                    stack.Children.Add(PropertyRow("Source document", item.SourceDocument));
                if (!string.IsNullOrWhiteSpace(item.SourceUrl))
                    stack.Children.Add(PropertyRow("Source URL", item.SourceUrl));
            }

            card.Child = stack;
            return card;
        }

        private static UIElement CreateInfoCard(string heading, string description)
        {
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = heading,
                FontWeight = FontWeights.SemiBold,
                Foreground = Ink,
                FontSize = 14
            });
            panel.Children.Add(new TextBlock
            {
                Text = description,
                Foreground = Muted,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 0),
                LineHeight = 19
            });

            return new Border
            {
                Background = Brushes.White,
                BorderBrush = Line,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(17),
                Margin = new Thickness(0, 0, 0, 13),
                Child = panel
            };
        }

        private static TextBlock SectionLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = Muted,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 13, 0, 5)
            };
        }

        private static UIElement PropertyRow(string label, string value)
        {
            var grid = new Grid { Margin = new Thickness(0, 3, 0, 3) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var labelText = new TextBlock
            {
                Text = label,
                Foreground = Muted,
                VerticalAlignment = VerticalAlignment.Top
            };
            var valueText = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(value) ? "Not available" : value,
                Foreground = Ink,
                TextWrapping = TextWrapping.Wrap
            };

            Grid.SetColumn(labelText, 0);
            Grid.SetColumn(valueText, 1);
            grid.Children.Add(labelText);
            grid.Children.Add(valueText);
            return grid;
        }

        private static UIElement ResultRow(string label, string result, Brush accent)
        {
            var grid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var labelText = new TextBlock
            {
                Text = label,
                Foreground = Muted,
                VerticalAlignment = VerticalAlignment.Center
            };

            var badge = CreateBadge(result, accent);
            badge.HorizontalAlignment = HorizontalAlignment.Left;

            Grid.SetColumn(labelText, 0);
            Grid.SetColumn(badge, 1);
            grid.Children.Add(labelText);
            grid.Children.Add(badge);
            return grid;
        }

        private static Border CreateBadge(string text)
        {
            return CreateBadge(text, StatusBrush(text));
        }

        private static Border CreateBadge(string text, Brush foreground)
        {
            return new Border
            {
                Background = new SolidColorBrush(ColorWithOpacity(foreground, 0.12)),
                CornerRadius = new CornerRadius(11),
                Padding = new Thickness(10, 4, 10, 4),
                Child = new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(text) ? "NOT AVAILABLE" : text,
                    Foreground = foreground,
                    FontSize = 10,
                    FontWeight = FontWeights.Bold
                }
            };
        }

        private static Brush StatusBrush(string status)
        {
            string value = (status ?? "").Trim().ToUpperInvariant();

            if (value == "PASS" || value == "EXACT MATCH" || value == "VERIFIED")
                return Green;

            if (value == "FAIL" || value == "NO EXACT MATCH")
                return Red;

            return Amber;
        }

        private static Color ColorWithOpacity(Brush brush, double opacity)
        {
            if (brush is SolidColorBrush solid)
                return Color.FromArgb((byte)(255 * opacity), solid.Color.R, solid.Color.G, solid.Color.B);

            return Color.FromArgb(30, 169, 107, 10);
        }

        private static SolidColorBrush BrushHex(string hex)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        }
    }
}
