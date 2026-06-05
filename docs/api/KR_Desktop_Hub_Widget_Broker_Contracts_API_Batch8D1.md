# KR Desktop Hub Widget Broker Contracts API

## Batch identity

`Stabilization Batch 8D1`

## Purpose

Widget broker contracts prevent future Widgets from receiving raw system objects by default.

## Enabled broker contracts

### `IWidgetClockBroker`

Requires:

```text
clock.read
```

The initial implementation returns a local-clock snapshot containing:

- current local time;
- local time-zone identifier;
- current offset from Coordinated Universal Time.

### `IWidgetNotificationBroker`

Requires:

```text
notification.send
```

The initial implementation calls a CoreHost-provided sender only after authorization succeeds.

## Reserved broker contracts

### `IWidgetHttpBroker`

Reserved for a future governed network layer.

### `IWidgetScopedFileBroker`

Reserved for future scoped file access.

Reserved means the contract exists for forward-compatible API design, but access is not enabled in the current release.

## Prohibited access

Widgets must not receive arbitrary shell or external-script execution capabilities.

```text
shell.execute
script.execute
```

remain prohibited.