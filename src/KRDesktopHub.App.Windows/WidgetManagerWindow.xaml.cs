using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using KRDesktopHub.Core;

namespace KRDesktopHub.App.Windows;

public partial class WidgetManagerWindow : Window
{
    private readonly InternalWidgetManagerService _defaultManager;

    private readonly Func<InternalWidgetManagerService>
        _developmentManagerFactory;

    private bool _operationInProgress;

    public WidgetManagerWindow(
        InternalWidgetManagerService defaultManager,
        Func<InternalWidgetManagerService> developmentManagerFactory)
    {
        ArgumentNullException.ThrowIfNull(
            defaultManager);

        ArgumentNullException.ThrowIfNull(
            developmentManagerFactory);

        _defaultManager =
            defaultManager;

        _developmentManagerFactory =
            developmentManagerFactory;

        InitializeComponent();

        RefreshInbox();
    }

    private void RefreshInboxButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        RefreshInbox();
    }

    private void BrowseArchiveButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var dialog =
            new OpenFileDialog
            {
                Title =
                    "Choose an Owner-approved KR Widget package",

                Filter =
                    "KR Widget packages (*.krwidget.zip)|*.krwidget.zip",

                CheckFileExists =
                    true,

                Multiselect =
                    false
            };

        if (dialog.ShowDialog(
            this)
            == true)
        {
            SelectedArchivePathTextBox.Text =
                dialog.FileName;

            SetStatus(
                "Package selected. Installation has not started.");
        }
    }

    private async void InstallInboxSelectionButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (InboxListBox.SelectedItem
            is not WidgetInboxArchiveInfo archive)
        {
            SetStatus(
                "Choose one discovered inbox package first.");

            return;
        }

        await RunInstallAsync(
            () =>
                _defaultManager.InstallInboxArchiveAsync(
                    archive.FullPath,
                    CancellationToken.None));
    }

    private async void InstallSelectedArchiveButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var archivePath =
            SelectedArchivePathTextBox.Text;

        if (string.IsNullOrWhiteSpace(
            archivePath))
        {
            SetStatus(
                "Choose a .krwidget.zip package file first.");

            return;
        }

        await RunInstallAsync(
            () =>
                _defaultManager.InstallSelectedArchiveAsync(
                    archivePath,
                    CancellationToken.None));
    }

    private async void InstallDevelopmentFolderButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (
            EnableDevelopmentFolderInstallCheckBox.IsChecked
            != true
        )
        {
            SetStatus(
                "Development-folder install remains disabled. Enable the Advanced checkbox only for an explicit development action.");

            return;
        }

        var dialog =
            new OpenFolderDialog
            {
                Title =
                    "Choose an Owner-approved development Widget folder",

                Multiselect =
                    false
            };

        if (dialog.ShowDialog(
            this)
            != true)
        {
            return;
        }

        var developmentManager =
            _developmentManagerFactory();

        await RunInstallAsync(
            () =>
                developmentManager.InstallDevelopmentFolderAsync(
                    dialog.FolderName,
                    CancellationToken.None));

        EnableDevelopmentFolderInstallCheckBox.IsChecked =
            false;
    }

    private void OpenPluginsFolderButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Directory.CreateDirectory(
            _defaultManager.PluginsDirectory);

        Process.Start(
            new ProcessStartInfo
            {
                FileName =
                    _defaultManager.PluginsDirectory,

                UseShellExecute =
                    true
            });

        SetStatus(
            "Plugins folder opened.");
    }

    private async Task RunInstallAsync(
        Func<Task<WidgetPackageInstallResult>> installAction)
    {
        ArgumentNullException.ThrowIfNull(
            installAction);

        if (_operationInProgress)
        {
            SetStatus(
                "Another Widget Manager operation is already running.");

            return;
        }

        _operationInProgress =
            true;

        ManagerRoot.IsEnabled =
            false;

        try
        {
            SetStatus(
                "Validating and installing package...");

            var result =
                await installAction();

            SetStatus(
                $"Installed {result.WidgetId} version {result.PackageVersion}. Source: {result.SourceMode}. Backup: {result.BackupPath ?? "none"}.");

            RefreshInbox();
        }
        catch (
            WidgetPackageValidationException exception)
        {
            SetStatus(
                $"Package rejected. Code: {exception.Code}. Reason: {exception.Message}");
        }
        catch (Exception exception)
        {
            SetStatus(
                $"Widget Manager operation failed: {exception.Message}");
        }
        finally
        {
            ManagerRoot.IsEnabled =
                true;

            _operationInProgress =
                false;
        }
    }

    private void RefreshInbox()
    {
        try
        {
            InboxListBox.ItemsSource =
                _defaultManager.RefreshInbox();

            SetStatus(
                "Inbox refreshed. Discovered files remain inert until you explicitly choose Install.");
        }
        catch (Exception exception)
        {
            SetStatus(
                $"Inbox refresh failed: {exception.Message}");
        }
    }

    private void SetStatus(
        string message)
    {
        StatusTextBlock.Text =
            message;
    }
}