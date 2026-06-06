
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KRDesktopHub.Contracts;

namespace KRDesktopHub.App.Windows;

public sealed record WidgetDialogOptionViewModel(
    string OptionId,
    string DisplayName,
    string? Description)
{
    public string DisplayText =>
        string.IsNullOrWhiteSpace(
            Description)
                ? DisplayName
                : DisplayName
                    + " — "
                    + Description;
}

public partial class WidgetDialogWindow
    : Window
{
    private readonly IReadOnlyList<
        WidgetDialogOptionViewModel> _options;

    public WidgetDialogWindow(
        WidgetDialogRequest request)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        InitializeComponent();

        Title =
            "KR Desktop Hub - "
            + request.Title;

        DialogTitleTextBlock.Text =
            request.Title;

        DialogMessageTextBlock.Text =
            request.Message
            ?? string.Empty;

        DialogMessageTextBlock.Visibility =
            string.IsNullOrWhiteSpace(
                request.Message)
                    ? Visibility.Collapsed
                    : Visibility.Visible;

        SearchTextBox.ToolTip =
            request.SearchPlaceholder
            ?? "Search";

        _options =
            request
                .Options
                .Select(
                    option =>
                        new WidgetDialogOptionViewModel(
                            option.OptionId,
                            option.DisplayName,
                            option.Description))
                .ToArray();

        ApplyFilter(
            string.Empty);
    }

    public WidgetDialogResult Result { get; private set; } =
        new(
            Accepted:
                false,

            SelectedOptionId:
                null);

    private void SearchTextBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        ApplyFilter(
            SearchTextBox.Text);
    }

    private void OptionsListBox_MouseDoubleClick(
        object sender,
        MouseButtonEventArgs e)
    {
        AcceptSelection();
    }

    private void AcceptButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        AcceptSelection();
    }

    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult =
            false;
    }

    private void ApplyFilter(
        string? query)
    {
        var normalized =
            query?.Trim()
            ?? string.Empty;

        OptionsListBox.ItemsSource =
            _options
                .Where(
                    option =>
                        string.IsNullOrWhiteSpace(
                            normalized)
                        || option.DisplayName.Contains(
                            normalized,
                            StringComparison.OrdinalIgnoreCase)
                        || (
                            option.Description?.Contains(
                                normalized,
                                StringComparison.OrdinalIgnoreCase)
                            ?? false
                        ))
                .OrderBy(
                    option =>
                        option.DisplayName,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        OptionsListBox.SelectedIndex =
            OptionsListBox.Items.Count > 0
                ? 0
                : -1;
    }

    private void AcceptSelection()
    {
        if (OptionsListBox.SelectedItem
            is not WidgetDialogOptionViewModel option)
        {
            return;
        }

        Result =
            new WidgetDialogResult(
                Accepted:
                    true,

                SelectedOptionId:
                    option.OptionId);

        DialogResult =
            true;
    }
}

public sealed class WindowsWidgetDialogPresenter
{
    private readonly Window _owner;

    public WindowsWidgetDialogPresenter(
        Window owner)
    {
        _owner =
            owner
            ?? throw new ArgumentNullException(
                nameof(owner));
    }

    public async Task<WidgetDialogResult> PresentAsync(
        string widgetId,
        WidgetDialogRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            widgetId);

        ArgumentNullException.ThrowIfNull(
            request);

        cancellationToken.ThrowIfCancellationRequested();

        if (_owner.Dispatcher.CheckAccess())
        {
            return PresentCore(
                request);
        }

        return await _owner
            .Dispatcher
            .InvokeAsync(
                () =>
                    PresentCore(
                        request))
            .Task;
    }

    private WidgetDialogResult PresentCore(
        WidgetDialogRequest request)
    {
        var dialog =
            new WidgetDialogWindow(
                request)
            {
                Owner =
                    _owner
            };

        _ =
            dialog.ShowDialog();

        return dialog.Result;
    }
}
