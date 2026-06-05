using System.ComponentModel;
using System.Windows;

namespace KRDesktopHub.App.Windows;

public partial class MainWindow : Window
{
    private bool _allowCloseAndExit;

    public bool CloseButtonHidesToTray { get; set; } = true;

    public event EventHandler? CloseExitRequested;

    public event EventHandler? WidgetManagerRequested;

    public MainWindow()
    {
        InitializeComponent();
    }

    public void AllowCloseAndExit()
    {
        _allowCloseAndExit = true;
    }

    private void OpenWidgetManagerButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        WidgetManagerRequested?.Invoke(
            this,
            EventArgs.Empty);
    }

    protected override void OnClosing(
        CancelEventArgs e)
    {
        if (!_allowCloseAndExit)
        {
            e.Cancel = true;

            if (CloseButtonHidesToTray)
            {
                Hide();
            }
            else
            {
                CloseExitRequested?.Invoke(
                    this,
                    EventArgs.Empty);
            }

            return;
        }

        base.OnClosing(e);
    }
}