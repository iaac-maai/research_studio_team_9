using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using SpecSynthAI.Models;
using SpecSynthAI.Services;

namespace SpecSynthAI.UI
{
    /// <summary>
    /// Unified Material Library with grouped import eligibility diagnostics.
    /// Import is still gated by RevitMaterialCreationService.
    /// </summary>
    public sealed class MaterialImporterWindow : Window
    {
        private ProductLibraryLoadResult? _loaded;
        private MaterialLibraryRecord? _selected;
        private MaterialLibraryVariant? _selectedVariant;

        public event Action<Product>? ImportRequested;

        private readonly TextBlock _dataset = new();
        private readonly TextBlock _summary = new();
        private readonly ListBox _records = new();
        private readonly ComboBox _variants = new();
        private readonly TextBox _rawJson = new();
        private readonly TextBlock _recordSummary = new();
        private readonly TextBlock _importStatus = new();
        private readonly Button _import = new()
        {
            Content = "Import into Revit",
            IsEnabled = false,
            Padding = new Thickness(16, 9, 16, 9)
        };

        public MaterialImporterWindow()
        {
            Title = "SpecSynth AI | Material Library";
            Width = 1380;
            Height = 850;
            MinWidth = 1050;
            MinHeight = 650;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush(Color.FromRgb(245, 247, 250));
            BuildUi();
            LoadDefault();
        }

        private void BuildUi()
        {
            var root = new Grid { Margin = new Thickness(16) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var header = new StackPanel();
            header.Children.Add(new TextBlock
            {
                Text = "Material Library", FontSize = 25, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(35, 48, 66))
            });
            header.Children.Add(new TextBlock
            {
                Text = "Unified product records, variants, evidence, completeness and Revit import eligibility.",
                Margin = new Thickness(0, 4, 0, 10), Foreground = Brushes.DimGray
            });
            var browse = new Button
            {
                Content = "Open product dataset…", Padding = new Thickness(12, 7, 12, 7),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            browse.Click += (_, _) => Browse();
            header.Children.Add(browse);
            header.Children.Add(_dataset);
            root.Children.Add(header);

            _summary.Margin = new Thickness(0, 12, 0, 12);
            _summary.FontWeight = FontWeights.SemiBold;
            Grid.SetRow(_summary, 1);
            root.Children.Add(_summary);

            var body = new Grid();
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.8, GridUnitType.Star) });
            body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });

            var left = new DockPanel { Margin = new Thickness(0, 0, 8, 0), Background = Brushes.White };
            var label = new TextBlock { Text = "PRODUCT RECORDS", FontWeight = FontWeights.Bold, Margin = new Thickness(10) };
            DockPanel.SetDock(label, Dock.Top);
            left.Children.Add(label);
            _records.Margin = new Thickness(8);
            _records.DisplayMemberPath = nameof(MaterialLibraryRecord.ProductName);
            _records.SelectionChanged += Selected;
            left.Children.Add(_records);
            Grid.SetColumn(left, 0);
            body.Children.Add(left);

            var right = new DockPanel { Margin = new Thickness(8, 0, 0, 0), Background = Brushes.White };
            var top = new StackPanel { Margin = new Thickness(10) };
            top.Children.Add(new TextBlock { Text = "PRODUCT DETAILS", FontWeight = FontWeights.Bold });
            _recordSummary.Margin = new Thickness(0, 8, 0, 8);
            _recordSummary.TextWrapping = TextWrapping.Wrap;
            top.Children.Add(_recordSummary);
            top.Children.Add(new TextBlock
            {
                Text = "SELECT PRODUCT THICKNESS / VARIANT", FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 8, 0, 4)
            });
            _variants.SelectionChanged += VariantSelected;
            top.Children.Add(_variants);

            top.Children.Add(new TextBlock
            {
                Text = "IMPORT ELIGIBILITY DIAGNOSTICS", FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 12, 0, 4)
            });
            _importStatus.Margin = new Thickness(0, 4, 0, 8);
            _importStatus.TextWrapping = TextWrapping.Wrap;
            top.Children.Add(_importStatus);
            _import.Click += (_, _) => RequestImport();
            top.Children.Add(_import);
            DockPanel.SetDock(top, Dock.Top);
            right.Children.Add(top);

            _rawJson.Margin = new Thickness(10);
            _rawJson.IsReadOnly = true;
            _rawJson.AcceptsReturn = true;
            _rawJson.AcceptsTab = true;
            _rawJson.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            _rawJson.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            _rawJson.FontFamily = new FontFamily("Consolas");
            _rawJson.FontSize = 12;
            _rawJson.TextWrapping = TextWrapping.NoWrap;
            right.Children.Add(_rawJson);

            Grid.SetColumn(right, 1);
            body.Children.Add(right);
            Grid.SetRow(body, 2);
            root.Children.Add(body);
            Content = root;
        }

        private void LoadDefault()
        {
            string assemblyDirectory = Path.GetDirectoryName(GetType().Assembly.Location) ?? "";
            string? path = new[]
            {
                Path.Combine(assemblyDirectory, "specsynth_products_seed_v1.0.0.json"),
                Path.Combine(assemblyDirectory, "products.json")
            }.FirstOrDefault(File.Exists);
            if (path != null) Load(path);
            else _dataset.Text = "No default dataset found. Select the unified JSON dataset.";
        }

        private void Browse()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select unified SpecSynth product dataset",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                CheckFileExists = true
            };
            if (dialog.ShowDialog(this) == true) Load(dialog.FileName);
        }

        private void Load(string path)
        {
            try
            {
                _loaded = ProductLibraryService.Load(path);
                _records.ItemsSource = _loaded.Records;
                _dataset.Text = $"Dataset: {_loaded.DatasetName} | Version: {_loaded.DatasetVersion} | {path}";
                _summary.Text =
                    $"Records: {_loaded.TotalProducts}    " +
                    $"Import eligible: {_loaded.ReadyForImport}    " +
                    $"Incomplete: {_loaded.Incomplete}    " +
                    $"Complete but blocked: {_loaded.NotVerifiedOrBlocked}";
                _records.SelectedIndex = -1;
                _selected = null;
                _selectedVariant = null;
                _variants.ItemsSource = null;
                _variants.IsEnabled = false;
                _rawJson.Clear();
                _recordSummary.Text = "Select a product to inspect its complete source record.";
                _importStatus.Text = "";
                _import.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Dataset loading failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Selected(object sender, SelectionChangedEventArgs e)
        {
            _selected = _records.SelectedItem as MaterialLibraryRecord;
            _selectedVariant = null;
            _variants.ItemsSource = null;
            _variants.IsEnabled = false;
            _import.IsEnabled = false;

            if (_selected == null)
            {
                _rawJson.Clear();
                _recordSummary.Text = "Select a product to inspect its complete source record.";
                _importStatus.Text = "";
                return;
            }

            _variants.ItemsSource = _selected.AvailableVariants;
            _variants.DisplayMemberPath = nameof(MaterialLibraryVariant.Display);
            _variants.IsEnabled = _selected.AvailableVariants.Count > 0;
            _variants.SelectedIndex = -1;
            DisplaySelectedRecord();
        }

        private void VariantSelected(object sender, SelectionChangedEventArgs e)
        {
            _selectedVariant = _variants.SelectedItem as MaterialLibraryVariant;
            if (_selected == null) return;
            DisplaySelectedRecord();
        }

        private void DisplaySelectedRecord()
        {
            if (_selected == null) return;
            _rawJson.Text = ProductLibraryService.FormatFullRecord(_selected);

            string completeness = _selected.CompletenessIssues.Count == 0
                ? "CORE IDENTITY / PARENT THERMAL FIELDS PRESENT"
                : "COMPLETENESS GAPS:\n• " + string.Join("\n• ", _selected.CompletenessIssues);

            string parentThickness = _selected.ThicknessMm.HasValue
                ? _selected.ThicknessMm.Value.ToString("0.##") + " mm"
                : "not supplied in parent record";
            string parentConductivity = _selected.ThermalConductivityWmK.HasValue
                ? _selected.ThermalConductivityWmK.Value.ToString("0.000") + " W/(m·K)"
                : "not supplied in parent record";

            string variantDetails = _selectedVariant == null
                ? "No catalogue variant selected."
                : $"Selected variant ID: {_selectedVariant.VariantId}\n" +
                  $"Selected catalogue thickness: {_selectedVariant.ThicknessMm:0.##} mm\n" +
                  $"Variant product code: {(string.IsNullOrWhiteSpace(_selectedVariant.ProductCode) ? "not supplied" : _selectedVariant.ProductCode)}\n" +
                  "Variant-specific thermal conductivity: not supplied in variant record\n" +
                  "Variant verification: not independently verified\n" +
                  "Note: Parent technical values are shown separately and are not copied into this variant.";

            _recordSummary.Text =
                "PARENT PRODUCT\n" +
                $"Product: {_selected.ProductName}\n" +
                $"Manufacturer: {_selected.Manufacturer}\n" +
                $"Product ID: {_selected.ProductId}\n" +
                $"Record type: {_selected.RecordType}\n" +
                $"Dataset verification.record_status: {(_selected.RecordStatus.Length == 0 ? "not supplied" : _selected.RecordStatus)}\n" +
                $"Revit material mapping: {(_selected.RevitMaterialName.Length == 0 ? "not supplied" : _selected.RevitMaterialName)}\n" +
                $"Parent thickness: {parentThickness}\n" +
                $"Parent thermal conductivity: {parentConductivity}\n\n" +
                "SELECTED VARIANT\n" + variantDetails + "\n\n" + completeness;

            var diagnostics = ProductLibraryService.EvaluateImportDiagnostics(_selected, _selectedVariant);
            _importStatus.Text = FormatDiagnostics(diagnostics);
            _importStatus.Foreground = diagnostics.Count == 0 ? Brushes.DarkGreen : Brushes.DarkRed;

            // Never enable the button based on the display-layer diagnostics alone.
            // Re-run the authoritative Revit validation immediately before enabling.
            MaterialImportValidationResult finalValidation =
                RevitMaterialCreationService.ValidateProduct(_selected.ImportProduct);
            _import.IsEnabled = diagnostics.Count == 0 && finalValidation.IsValid && _selectedVariant == null;
        }

        private static string FormatDiagnostics(System.Collections.Generic.List<ImportDiagnostic> diagnostics)
        {
            if (diagnostics.Count == 0)
                return "ELIGIBLE — all library checks passed. Final Revit validation will run again before import.";

            var output = new StringBuilder();
            string[] groups = { "Identity", "Classification", "Technical data", "Manufacturer verification", "SKU / variant", "Revit material mapping", "Revit importer validation" };
            foreach (string group in groups)
            {
                var entries = diagnostics.Where(d => d.Group == group).ToList();
                if (entries.Count == 0) continue;
                output.AppendLine(group.ToUpperInvariant() + ":");
                foreach (ImportDiagnostic item in entries)
                    output.AppendLine("• " + item.Message);
            }
            return "NOT ELIGIBLE FOR REVIT IMPORT:\n" + output.ToString().TrimEnd();
        }

        private void RequestImport()
        {
            if (_selected == null || !_import.IsEnabled) return;
            MaterialImportValidationResult validation =
                RevitMaterialCreationService.ValidateProduct(_selected.ImportProduct);
            if (!validation.IsValid)
            {
                _importStatus.Text = "REJECTED BY FINAL REVIT VALIDATION:\n• " + string.Join("\n• ", validation.Errors);
                _importStatus.Foreground = Brushes.DarkRed;
                _import.IsEnabled = false;
                return;
            }
            _import.IsEnabled = false;
            ImportRequested?.Invoke(_selected.ImportProduct);
        }
    }
}
