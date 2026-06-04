using KRDesktopHub.Contracts;
using KRDesktopHub.Platform.Abstractions;

var descriptor = new WidgetDescriptor(
    "kr.fixture.contract-smoke",
    "Contract Smoke Test",
    new Version(0, 1, 0),
    new Version(1, 0, 0),
    new Version(0, 1, 0),
    ["scheduled_refresh", "local_storage"]);

if (descriptor.WidgetId != "kr.fixture.contract-smoke")
{
    throw new InvalidOperationException("WidgetDescriptor validation failed.");
}

if (!typeof(IKrWidget).IsInterface)
{
    throw new InvalidOperationException("IKrWidget contract missing.");
}

if (!typeof(ITrayService).IsInterface)
{
    throw new InvalidOperationException("ITrayService contract missing.");
}

if (!Enum.IsDefined(WidgetActivationMode.ScheduledWindow))
{
    throw new InvalidOperationException("WidgetActivationMode contract missing.");
}

Console.WriteLine("Batch 1 contract smoke test passed.");