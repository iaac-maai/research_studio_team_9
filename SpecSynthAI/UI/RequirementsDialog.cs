using SpecSynthAI.Requirements;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SpecSynthAI.UI
{
    public class RequirementsDialog : Window
    {
        private readonly TextBox _conductivityBox;
        private readonly TextBox _externalUValueBox;
        private readonly TextBox _internalUValueBox;
        private readonly TextBox _toleranceBox;

        private readonly TextBlock _statusText;
        private readonly Border _statusIndicator;

        private static readonly Brush Navy = BrushFromHex("#142B45");
        private static readonly Brush Teal = BrushFromHex("#087E8B");
        private static readonly Brush PageBackground = BrushFromHex("#F3F6FA");
        private static readonly Brush MainText = BrushFromHex("#243447");
        private static readonly Brush MutedText = BrushFromHex("#68788A");
        private static readonly Brush BorderColor = BrushFromHex("#DCE4ED");
        private static readonly Brush ErrorRed = BrushFromHex("#C0392B");

        public RequirementsDialog()
        {
            Title = "SpecSynth AI | Project Requirements";
            Width = 720;
            Height = 760;
            MinWidth = 650;
            MinHeight = 600;

            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.CanResize;
            ShowInTaskbar = false;

            Background = PageBackground;
            FontFamily = new FontFamily("Segoe UI");
            FontSize = 13;

            // Load existing saved requirements.
            _conductivityBox = CreateTextBox(
                ProjectRequirementsStore.LoadThermalConductivity()
                    .MaximumAllowedValue);

            _externalUValueBox = CreateTextBox(
                ProjectRequirementsStore.LoadExternalWallUValue()
                    .MaximumAllowedValue);

            _internalUValueBox = CreateTextBox(
                ProjectRequirementsStore.LoadInternalWallUValue()
                    .MaximumAllowedValue);

            _toleranceBox = CreateTextBox(
                ProjectRequirementsStore.LoadModerateTolerance()
                    .MaximumAllowedValue);

            var root = new Grid();

            root.RowDefinitions.Add(
                new RowDefinition { Height = GridLength.Auto });

            root.RowDefinitions.Add(
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            root.RowDefinitions.Add(
                new RowDefinition { Height = GridLength.Auto });

            Content = root;

            // HEADER
            var header = new Border
            {
                Background = Navy,
                Padding = new Thickness(28, 22, 28, 22)
            };

            var headerStack = new StackPanel();

            headerStack.Children.Add(new TextBlock
            {
                Text = "SPECSYNTH AI",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = BrushFromHex("#8ED3D6")
            });

            headerStack.Children.Add(new TextBlock
            {
                Text = "Project Requirements",
                FontSize = 25,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 7, 0, 4)
            });

            headerStack.Children.Add(new TextBlock
            {
                Text = "Configure product and building thermal performance criteria.",
                FontSize = 13,
                Foreground = BrushFromHex("#D6E1EB"),
                TextWrapping = TextWrapping.Wrap
            });

            header.Child = headerStack;

            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // SCROLLABLE CONTENT
            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(28, 22, 28, 18)
            };

            var content = new StackPanel();

            content.Children.Add(new TextBlock
            {
                Text = "THERMAL REQUIREMENT CONFIGURATION",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = MutedText,
                Margin = new Thickness(0, 0, 0, 14)
            });

            // PRODUCT REQUIREMENTS
            content.Children.Add(CreateSectionHeading(
                "01  PRODUCT PERFORMANCE"));

            content.Children.Add(CreateRequirementCard(
                "Maximum thermal conductivity",
                "Maximum permitted conductivity of construction products.",
                _conductivityBox,
                "W/(m·K)"));

            // BUILDING REQUIREMENTS
            content.Children.Add(CreateSectionHeading(
                "02  BUILDING THERMAL PERFORMANCE"));

            content.Children.Add(CreateRequirementCard(
                "Maximum external wall U-value",
                "Maximum permitted U-value for external wall assemblies.",
                _externalUValueBox,
                "W/(m²·K)"));

            content.Children.Add(CreateRequirementCard(
                "Maximum internal wall U-value",
                "Maximum permitted U-value for internal wall assemblies.",
                _internalUValueBox,
                "W/(m²·K)"));

            content.Children.Add(CreateRequirementCard(
                "Moderate-performance tolerance",
                "Percentage above the target allowed before classification as Poor.",
                _toleranceBox,
                "%"));

            // INFORMATION
            var infoCard = new Border
            {
                Background = Brushes.White,
                BorderBrush = BorderColor,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 4, 0, 12)
            };

            infoCard.Child = new TextBlock
            {
                Text =
                    "These are project-defined prototype requirements. " +
                    "They are not universal regulatory limits or proof of " +
                    "complete building-code compliance. Ensure the values " +
                    "are appropriate for the project before relying on them.",
                Foreground = MutedText,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 19
            };

            content.Children.Add(infoCard);

            // STATUS
            var statusRow = new DockPanel
            {
                LastChildFill = true,
                Margin = new Thickness(0, 5, 0, 5)
            };

            _statusIndicator = new Border
            {
                Width = 4,
                Background = Teal,
                CornerRadius = new CornerRadius(2),
                Margin = new Thickness(0, 2, 10, 2)
            };

            DockPanel.SetDock(_statusIndicator, Dock.Left);
            statusRow.Children.Add(_statusIndicator);

            _statusText = new TextBlock
            {
                Text = "Edit the requirements and save your changes.",
                Foreground = MutedText,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12
            };

            statusRow.Children.Add(_statusText);
            content.Children.Add(statusRow);

            scroll.Content = content;

            Grid.SetRow(scroll, 1);
            root.Children.Add(scroll);

            // FOOTER
            var footer = new Border
            {
                Background = Brushes.White,
                BorderBrush = BorderColor,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(24, 14, 24, 14)
            };

            var buttons = new DockPanel
            {
                LastChildFill = false
            };

            var cancelButton = CreateButton(
                "Cancel",
                Brushes.White,
                MainText,
                BorderColor);

            cancelButton.MinWidth = 90;
            cancelButton.IsCancel = true;
            cancelButton.Click += (_, _) => DialogResult = false;

            DockPanel.SetDock(cancelButton, Dock.Right);
            buttons.Children.Add(cancelButton);

            var saveButton = CreateButton(
                "Save Requirements",
                Teal,
                Brushes.White,
                Teal);

            saveButton.MinWidth = 165;
            saveButton.Margin = new Thickness(0, 0, 9, 0);
            saveButton.IsDefault = true;
            saveButton.Click += SaveButton_Click;

            DockPanel.SetDock(saveButton, Dock.Right);
            buttons.Children.Add(saveButton);

            var resetButton = CreateButton(
                "Reset Defaults",
                Brushes.Transparent,
                Teal,
                Brushes.Transparent);

            resetButton.Click += ResetButton_Click;

            DockPanel.SetDock(resetButton, Dock.Left);
            buttons.Children.Add(resetButton);

            footer.Child = buttons;

            Grid.SetRow(footer, 2);
            root.Children.Add(footer);
        }

        private static TextBox CreateTextBox(double value)
        {
            return new TextBox
            {
                Text = value.ToString(
                    "0.###",
                    CultureInfo.InvariantCulture),

                FontSize = 17,
                FontWeight = FontWeights.SemiBold,
                Foreground = MainText,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0, 7, 0, 7),
                VerticalContentAlignment = VerticalAlignment.Center,
                MinWidth = 150
            };
        }

        private static TextBlock CreateSectionHeading(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Teal,
                Margin = new Thickness(0, 8, 0, 10)
            };
        }

        private static Border CreateRequirementCard(
            string title,
            string description,
            TextBox input,
            string unit)
        {
            var card = new Border
            {
                Background = Brushes.White,
                BorderBrush = BorderColor,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(17),
                Margin = new Thickness(0, 0, 0, 12)
            };

            var stack = new StackPanel();

            stack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = MainText
            });

            stack.Children.Add(new TextBlock
            {
                Text = description,
                FontSize = 11,
                Foreground = MutedText,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 5, 0, 12)
            });

            var inputGrid = new Grid();

            inputGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });

            inputGrid.ColumnDefinitions.Add(
                new ColumnDefinition { Width = GridLength.Auto });

            var inputBorder = new Border
            {
                BorderBrush = BorderColor,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 2, 10, 2),
                Child = input
            };

            inputGrid.Children.Add(inputBorder);

            var unitBorder = new Border
            {
                Background = PageBackground,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 0, 12, 0),
                Margin = new Thickness(9, 0, 0, 0),
                Child = new TextBlock
                {
                    Text = unit,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = MainText,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            Grid.SetColumn(unitBorder, 1);
            inputGrid.Children.Add(unitBorder);

            stack.Children.Add(inputGrid);

            card.Child = stack;

            return card;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryReadValue(_conductivityBox, out double conductivity) ||
                !TryReadValue(_externalUValueBox, out double externalUValue) ||
                !TryReadValue(_internalUValueBox, out double internalUValue) ||
                !TryReadValue(_toleranceBox, out double tolerance))
            {
                SetStatus(
                    "Enter valid positive numbers in all four fields.",
                    ErrorRed);

                return;
            }

            if (tolerance > 100)
            {
                SetStatus(
                    "Moderate-performance tolerance cannot exceed 100%.",
                    ErrorRed);

                _toleranceBox.Focus();
                _toleranceBox.SelectAll();

                return;
            }

            try
            {
                ProjectRequirementsStore.SaveThermalConductivity(conductivity);
                ProjectRequirementsStore.SaveExternalWallUValue(externalUValue);
                ProjectRequirementsStore.SaveInternalWallUValue(internalUValue);
                ProjectRequirementsStore.SaveModerateTolerance(tolerance);

                SetStatus("All project requirements saved successfully.", Teal);

                MessageBox.Show(
                    this,
                    "Project requirements saved successfully.\n\n" +
                    "Configuration file:\n" +
                    ProjectRequirementsStore.FilePath,
                    "SpecSynth AI",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
            }
            catch (Exception ex)
            {
                SetStatus(
                    "Could not save requirements: " + ex.Message,
                    ErrorRed);
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _conductivityBox.Text = "0.022";
            _externalUValueBox.Text = "0.30";
            _internalUValueBox.Text = "0.60";
            _toleranceBox.Text = "10";

            SetStatus(
                "Defaults restored in the form. Click Save Requirements to apply them.",
                Teal);
        }

        private static bool TryReadValue(
            TextBox textBox,
            out double value)
        {
            string input = textBox.Text.Trim();

            bool parsed =
                double.TryParse(
                    input,
                    NumberStyles.Float,
                    CultureInfo.CurrentCulture,
                    out value);

            if (!parsed)
            {
                parsed = double.TryParse(
                    input,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out value);
            }

            return parsed &&
                   !double.IsNaN(value) &&
                   !double.IsInfinity(value) &&
                   value > 0;
        }

        private void SetStatus(string message, Brush accent)
        {
            _statusText.Text = message;
            _statusText.Foreground = accent;
            _statusIndicator.Background = accent;
        }

        private static Button CreateButton(
            string text,
            Brush background,
            Brush foreground,
            Brush border)
        {
            var button = new Button
            {
                Content = text,
                Background = background,
                Foreground = foreground,
                BorderBrush = border,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 9, 14, 9),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            button.Template = CreateButtonTemplate();

            return button;
        }

        private static ControlTemplate CreateButtonTemplate()
        {
            var template = new ControlTemplate(typeof(Button));

            var borderFactory =
                new FrameworkElementFactory(typeof(Border));

            borderFactory.SetValue(
                Border.CornerRadiusProperty,
                new CornerRadius(6));

            borderFactory.SetValue(
                Border.BackgroundProperty,
                new TemplateBindingExtension(Control.BackgroundProperty));

            borderFactory.SetValue(
                Border.BorderBrushProperty,
                new TemplateBindingExtension(Control.BorderBrushProperty));

            borderFactory.SetValue(
                Border.BorderThicknessProperty,
                new TemplateBindingExtension(Control.BorderThicknessProperty));

            var contentFactory =
                new FrameworkElementFactory(typeof(ContentPresenter));

            contentFactory.SetValue(
                ContentPresenter.HorizontalAlignmentProperty,
                HorizontalAlignment.Center);

            contentFactory.SetValue(
                ContentPresenter.VerticalAlignmentProperty,
                VerticalAlignment.Center);

            contentFactory.SetValue(
                ContentPresenter.MarginProperty,
                new TemplateBindingExtension(Control.PaddingProperty));

            borderFactory.AppendChild(contentFactory);
            template.VisualTree = borderFactory;

            return template;
        }

        private static SolidColorBrush BrushFromHex(string hex)
        {
            return new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(hex));
        }
    }
}
