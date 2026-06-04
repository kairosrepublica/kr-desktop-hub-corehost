using System.ComponentModel;
using System.Windows;

namespace KRDesktopHub.App.Windows;

public partial class MainWindow : Window
{
    private bool _allowCloseAndExit;

    public MainWindow()
    {
        InitializeComponent();
    }

    public void AllowCloseAndExit()
    {
        _allowCloseAndExit = true;
    }

    protected override void OnClosing(
        CancelEventArgs e)
    {
        if (!_allowCloseAndExit)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnClosing(e);
    }
}