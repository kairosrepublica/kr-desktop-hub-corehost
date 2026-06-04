using KRDesktopHub.Contracts;
using KRDesktopHub.WidgetSdk;

namespace KRDesktopHub.HelloWidget;

public sealed class HelloWidget
    : KrWidgetBase
{
    public override WidgetDescriptor Descriptor { get; } =
        new(
            "kr.sample.hello",
            "Hello Widget",
            new Version(0, 1, 0),
            new Version(1, 0, 0),
            new Version(0, 1, 0),
            new[]
            {
                "sample",
                "state_store"
            });

    protected override async Task OnStartAsync(
        CancellationToken cancellationToken)
    {
        var count =
            await ReadStateAsync<int>(
                "hello.start_count",
                cancellationToken);

        await WriteStateAsync(
            "hello.start_count",
            count + 1,
            cancellationToken);

        Context.Logger.Information(
            "Hello Widget started.");
    }
}