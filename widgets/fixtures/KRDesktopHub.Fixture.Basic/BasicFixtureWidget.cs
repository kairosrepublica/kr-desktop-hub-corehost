using KRDesktopHub.Contracts;

namespace KRDesktopHub.Fixture.Basic;

public sealed class BasicFixtureWidget
    : IKrWidget
{
    private IWidgetContext? _context;

    public WidgetDescriptor Descriptor { get; } =
        new(
            "kr.fixture.basic",
            "Basic Fixture Widget",
            new Version(0, 1, 0),
            new Version(1, 0, 0),
            new Version(0, 1, 0),
            new[]
            {
                "lifecycle",
                "state_store"
            });

    public async Task InitializeAsync(
        IWidgetContext context,
        CancellationToken cancellationToken)
    {
        _context = context;

        await WriteLifecycleAsync(
            "initialized",
            cancellationToken);
    }

    public Task StartAsync(
        CancellationToken cancellationToken)
    {
        return WriteLifecycleAsync(
            "running",
            cancellationToken);
    }

    public Task PauseAsync(
        CancellationToken cancellationToken)
    {
        return WriteLifecycleAsync(
            "paused",
            cancellationToken);
    }

    public Task ResumeAsync(
        CancellationToken cancellationToken)
    {
        return WriteLifecycleAsync(
            "running",
            cancellationToken);
    }

    public Task StopAsync(
        CancellationToken cancellationToken)
    {
        return WriteLifecycleAsync(
            "stopped",
            cancellationToken);
    }

    private Task WriteLifecycleAsync(
        string value,
        CancellationToken cancellationToken)
    {
        return (_context
            ?? throw new InvalidOperationException(
                "Widget context is unavailable."))
            .StateStore
            .WriteAsync(
                "fixture.lifecycle",
                value,
                cancellationToken);
    }
}