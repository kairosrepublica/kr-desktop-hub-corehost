using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using KRDesktopHub.Core;
using KRDesktopHub.Platform.Windows;

namespace KRDesktopHub.App.Windows;

public partial class SettingsCenterWindow : Window
{
    private readonly SettingsCenterRuntimeBridge _bridge;

    private readonly Dictionary<string, Control> _editors =
        new(
            StringComparer.Ordinal);

    private CoreHostSettingsCenterDocument _document;

    public SettingsCenterWindow(
        SettingsCenterRuntimeBridge bridge)
    {
        ArgumentNullException.ThrowIfNull(
            bridge);

        _bridge =
            bridge;

        _document =
            _bridge.LoadOrCreate();

        InitializeComponent();

        BuildInterface();

        LoadValues();
    }

    public event EventHandler? SettingsSaved;

    private void BuildInterface()
    {
        foreach (var section in
            CoreHostSettingsCenterCatalog
                .All
                .GroupBy(
                    descriptor =>
                        descriptor.SectionId,
                    StringComparer.Ordinal))
        {
            var sectionPanel =
                new StackPanel
                {
                    Margin =
                        new Thickness(
                            0,
                            0,
                            0,
                            18)
                };

            sectionPanel.Children.Add(
                new TextBlock
                {
                    FontSize =
                        16,

                    FontWeight =
                        FontWeights.SemiBold,

                    Text =
                        section.Key
                });

            foreach (var descriptor in
                section)
            {
                sectionPanel.Children.Add(
                    CreateEditor(
                        descriptor));
            }

            SectionsPanel.Children.Add(
                sectionPanel);
        }
    }

    private FrameworkElement CreateEditor(
        CoreHostSettingDescriptor descriptor)
    {
        var property =
            typeof(
                    CoreHostSettingsCenterState)
                .GetProperty(
                    descriptor.Key,
                    BindingFlags.Instance
                    | BindingFlags.Public)
            ?? throw new InvalidOperationException(
                $"Unknown settings property: {descriptor.Key}");

        var container =
            new Border
            {
                BorderBrush =
                    SystemColors.ControlDarkBrush,

                BorderThickness =
                    new Thickness(
                        1),

                Margin =
                    new Thickness(
                        0,
                        8,
                        0,
                        0),

                Padding =
                    new Thickness(
                        10)
            };

        var panel =
            new StackPanel();

        panel.Children.Add(
            new TextBlock
            {
                FontWeight =
                    FontWeights.SemiBold,

                Text =
                    descriptor.DisplayName
            });

        panel.Children.Add(
            new TextBlock
            {
                Margin =
                    new Thickness(
                        0,
                        4,
                        0,
                        0),

                TextWrapping =
                    TextWrapping.Wrap,

                Text =
                    descriptor.Description
            });

        panel.Children.Add(
            new TextBlock
            {
                Margin =
                    new Thickness(
                        0,
                        4,
                        0,
                        0),

                TextWrapping =
                    TextWrapping.Wrap,

                Text =
                    $"Recommended: {descriptor.RecommendedDefault}. Reason: {descriptor.RecommendationReason}"
            });

        panel.Children.Add(
            new TextBlock
            {
                Margin =
                    new Thickness(
                        0,
                        4,
                        0,
                        6),

                Text =
                    $"Application mode: {descriptor.ApplyMode}"
            });

        Control editor;

        if (property.PropertyType
            == typeof(
                bool))
        {
            editor =
                new CheckBox();
        }
        else
        {
            editor =
                new TextBox
                {
                    MinWidth =
                        240
                };
        }

        _editors[descriptor.Key] =
            editor;

        panel.Children.Add(
            editor);

        container.Child =
            panel;

        return container;
    }

    private void SaveButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            SaveEditorValues();

            _bridge.Save(
                _document);

            SettingsSaved?.Invoke(
                this,
                EventArgs.Empty);

            SetStatus(
                "Settings saved and active runtime settings reloaded.");
        }
        catch (
            CoreHostSettingsValidationException exception)
        {
            SetStatus(
                $"Validation failed: {string.Join(" | ", exception.Errors)}");
        }
        catch (Exception exception)
        {
            SetStatus(
                $"Save failed: {exception.Message}");
        }
    }

    private void ReloadButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            _document =
                _bridge.LoadOrCreate();

            LoadValues();

            SetStatus(
                "Settings reloaded from the active runtime source.");
        }
        catch (Exception exception)
        {
            SetStatus(
                $"Reload failed: {exception.Message}");
        }
    }

    private void OpenSettingsFolderButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Directory.CreateDirectory(
            _bridge.SettingsDirectory);

        Process.Start(
            new ProcessStartInfo
            {
                FileName =
                    _bridge.SettingsDirectory,

                UseShellExecute =
                    true
            });

        SetStatus(
            "Settings folder opened.");
    }

    private void LoadValues()
    {
        foreach (var pair in
            _editors)
        {
            var property =
                GetSettingsProperty(
                    pair.Key);

            var value =
                property.GetValue(
                    _document.Settings);

            if (
                pair.Value
                is CheckBox checkBox
            )
            {
                checkBox.IsChecked =
                    value
                    as bool?
                    ?? false;
            }
            else if (
                pair.Value
                is TextBox textBox
            )
            {
                textBox.Text =
                    Convert.ToString(
                        value)
                    ?? "";
            }
        }
    }

    private void SaveEditorValues()
    {
        foreach (var pair in
            _editors)
        {
            var property =
                GetSettingsProperty(
                    pair.Key);

            if (
                pair.Value
                is CheckBox checkBox
            )
            {
                property.SetValue(
                    _document.Settings,
                    checkBox.IsChecked
                    == true);
            }
            else if (
                pair.Value
                is TextBox textBox
            )
            {
                object value =
                    property.PropertyType
                    == typeof(
                        int)
                        ? int.Parse(
                            textBox.Text)
                        : textBox.Text;

                property.SetValue(
                    _document.Settings,
                    value);
            }
        }
    }

    private static PropertyInfo GetSettingsProperty(
        string key)
    {
        return typeof(
                CoreHostSettingsCenterState)
            .GetProperty(
                key,
                BindingFlags.Instance
                | BindingFlags.Public)
            ?? throw new InvalidOperationException(
                $"Unknown settings property: {key}");
    }

    private void SetStatus(
        string message)
    {
        StatusTextBlock.Text =
            message;
    }
}