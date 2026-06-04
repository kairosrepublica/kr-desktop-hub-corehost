# KR Desktop Hub Core Runtime Usage â€” Batch 2

## Create runtime services

```csharp
var runtime = CoreRuntimeFactory.Create(
    dataRoot,
    resourceDirectory,
    settingsFile);
```

## Use event bus

```csharp
using var subscription = runtime.EventBus.Subscribe<MyEvent>(
    async (message, cancellationToken) =>
    {
        await HandleAsync(message, cancellationToken);
    });

await runtime.EventBus.PublishAsync(
    new MyEvent(),
    cancellationToken);
```

## Use state store

```csharp
await runtime.StateStore.WriteAsync(
    "key",
    value,
    cancellationToken);

var value = await runtime.StateStore.ReadAsync<MyType>(
    "key",
    cancellationToken);
```

## Use commands

```csharp
runtime.Commands.Register(
    command,
    handler);

await runtime.Commands.ExecuteAsync(
    command.CommandId,
    cancellationToken);
```