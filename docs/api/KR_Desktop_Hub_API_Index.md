# KR Desktop Hub API Index

## Widget development

```text
KR_Desktop_Hub_Widget_API_Batch1.md
KR_Desktop_Hub_Widget_Runtime_API_Batch4.md
KR_Desktop_Hub_Widget_Manifest_Schema_Batch4.json
KR_Desktop_Hub_Widget_SDK_API_Batch6.md
```

## Core Runtime

```text
KR_Desktop_Hub_Core_Runtime_API_Batch2.md
KR_Desktop_Hub_Core_Runtime_Usage_Batch2.md
KR_Desktop_Hub_System_Policies_API_Batch5.md
KR_Desktop_Hub_System_Policies_Usage_Batch5.md
KR_Desktop_Hub_Diagnostics_And_Migration_API_Batch6.md
```

## Windows adapter

```text
KR_Desktop_Hub_Platform_Abstractions_API_Batch1.md
KR_Desktop_Hub_Windows_Shell_API_Batch3.md
```

## Release

```text
../release/KR_Desktop_Hub_CoreHost_Portable_RC1_Release_Notes.md
../release/KR_Desktop_Hub_CoreHost_Portable_Manual_Acceptance_Checklist.md
```
## Window placement

```text
KR_Desktop_Hub_Window_Placement_API_Batch8A.md
```

## CoreHost settings

```text
KR_Desktop_Hub_CoreHost_Settings_API_Batch8B1.md
```

## System policy and notification governance

```text
KR_Desktop_Hub_System_Policy_And_Notification_Governance_API_Batch8B2A.md
```
- [Widget Runtime Execution Policy API - Batch 8B2B](KR_Desktop_Hub_Widget_Runtime_Execution_Policy_API_Batch8B2B.md)
- [Internal Widget Package Installer API - Batch 8C1](KR_Desktop_Hub_Widget_Package_Installer_API_Batch8C1.md)
- [Internal Widget Manager API - Batch 8C2](KR_Desktop_Hub_Widget_Manager_API_Batch8C2.md)
- [Widget Capability Governance API - Batch 8D1](KR_Desktop_Hub_Widget_Capability_Governance_API_Batch8D1.md)
- [Widget Broker Contracts API - Batch 8D1](KR_Desktop_Hub_Widget_Broker_Contracts_API_Batch8D1.md)
- [Settings Center API - Batch 8D2](KR_Desktop_Hub_Settings_Center_API_Batch8D2.md)


## Universal Widget framework foundation

```text
KR_Desktop_Hub_Universal_Widget_Framework_API_v1.0.md
KR_Desktop_Hub_Widget_Package_Manifest_Schema_v1.1.json
```


## Installed Widget activation backend

```text
KR_Desktop_Hub_Installed_Widget_Catalog_API_v1.0.md
KR_Desktop_Hub_Widget_Package_Manifest_Schema_v1.2.json
```
- [Checkpoint 2B Windows Widget Composition API](KR_Desktop_Hub_Checkpoint2B_Windows_Widget_Composition_API_v1.0.md)

## CoreHost stabilization

```text
../architecture/KR_Desktop_Hub_CoreHost_Stabilization_Settings_And_Collapse_v1.0.md
```


## CoreHost snapped-shell and Widget-refresh stabilization

```text
../architecture/KR_Desktop_Hub_CoreHost_Stabilization_WindowSnap_And_WidgetRefresh_v1.0.md
```

## Owner-sized popup viewport policy

See:

```text
docs/architecture/
KR_Desktop_Hub_CoreHost_Stabilization_OwnerSizedViewport_And_WidgetRefresh_v1.0.md
```

## CoreHost state-only Widget-host transition checkpoint

Architecture note:

```text
docs/architecture/KR_Desktop_Hub_CoreHost_StateOnly_WidgetHost_Transitions_v1.0.md
```

Key framework seam:

```text
WidgetHostChromePresentation
WidgetHostChromeTransitionController
InstalledWidgetCatalogProjection
InstalledWidgetHostCompositionCoordinator.SynchronizeStateAsync
```

## CoreHost transactional installed-catalog refresh checkpoint

Architecture note:

```text
docs/architecture/KR_Desktop_Hub_CoreHost_Transactional_Installed_Catalog_Refresh_v1.0.md
```

Key seams:

```text
InstalledWidgetCatalogCandidate
InstalledWidgetCatalogService.DiscoverAsync
InstalledWidgetCatalogService.CommitAcceptedCandidate
WidgetHostLayoutController.ReconcileActiveRegistrations
InMemoryWidgetCapabilityApprovalStore.ReconcileApprovedCapabilities
GovernedWidgetTrayIconBroker.RevokeRequestsExceptAsync
WindowsWidgetFrameworkServices.SynchronizeApprovedCapabilitiesAsync
```
