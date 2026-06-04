# KR Desktop Hub System Policies Usage â€” Batch 5

## Evaluate a Widget

```csharp
var evaluator =
    new SystemPolicyEvaluator(
        CoreHostPolicyOptions.Recommended);

var decision =
    evaluator.Evaluate(
        widgetProfile,
        systemState);
```

## Subscribe Windows adapters

```csharp
using var power =
    new WindowsPowerStateService();

using var network =
    new WindowsNetworkStateService();

using var session =
    new WindowsSessionStateService();

using var timeZone =
    new WindowsTimeZoneChangeService();

await using var resources =
    new WindowsProcessResourceMonitorService(
        TimeSpan.FromSeconds(30));
```

## Bridge events into the Core Runtime

```csharp
using var coordinator =
    new SystemPolicyCoordinator(
        eventBus,
        power,
        network,
        session,
        timeZone,
        resources);

await resources.StartAsync(
    cancellationToken);
```