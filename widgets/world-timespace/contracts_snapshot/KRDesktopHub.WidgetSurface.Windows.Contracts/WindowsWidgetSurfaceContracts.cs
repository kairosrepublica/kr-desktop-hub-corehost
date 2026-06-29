using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace KRDesktopHub.WidgetSurface.Windows.Contracts;

public static class WindowsWidgetSurfaceContractIdentity
{
    public const int ManifestSchemaVersion = 2;

    public static readonly Version ContractVersion =
        new(2, 5, 0);

    public const string AssemblyName =
        "KRDesktopHub.WidgetSurface.Windows.Contracts";
}

public sealed record WindowsWidgetSurfaceMountContext(
    string WidgetId,
    double InitialHostWidthDip,
    bool Collapsed,
    IWindowsWidgetDesiredHeightSink DesiredHeightSink,
    IWindowsWidgetSelfActionSink SelfActionSink)
{
    public IWindowsWidgetNetworkReadBroker? Network { get; init; }
}


public sealed record WindowsWidgetNetworkReadRequest(
    Uri Uri,
    IReadOnlyDictionary<string, string>? Headers = null);

public sealed record WindowsWidgetNetworkReadResponse(
    Uri Uri,
    int StatusCode,
    IReadOnlyDictionary<string, string[]> Headers,
    byte[] Body);

public interface IWindowsWidgetNetworkReadBroker
{
    ValueTask<WindowsWidgetNetworkReadResponse> ReadAsync(
        WindowsWidgetNetworkReadRequest request,
        CancellationToken cancellationToken);
}

public sealed record WindowsWidgetSurfaceDetachContext(
    string WidgetId,
    string Reason,
    DateTimeOffset StartedAtUtc);

public interface IWindowsWidgetDesiredHeightSink
{
    void ReportDesiredHeight(
        string widgetId,
        double desiredHeightDip);
}

public interface IWindowsWidgetSelfActionSink
{
    void RequestCollapsedState(
        bool collapsed);

    void RequestCloseSelf();
}

public interface IWindowsWidgetSurfaceFactory
{
    string WidgetId { get; }

    ValueTask<IWindowsWidgetSurfaceLease> CreateSurfaceAsync(
        WindowsWidgetSurfaceMountContext context,
        CancellationToken cancellationToken);
}

public interface IWindowsWidgetSurfaceLease
    : IAsyncDisposable
{
    string WidgetId { get; }

    FrameworkElement RootElement { get; }

    ValueTask PrepareForDetachAsync(
        WindowsWidgetSurfaceDetachContext context,
        CancellationToken cancellationToken);
}

public interface IWindowsWidgetSurfaceHostStateSink
{
    void ApplyHostWidth(
        double widthDip);

    void ApplyCollapsed(
        bool collapsed);
}
