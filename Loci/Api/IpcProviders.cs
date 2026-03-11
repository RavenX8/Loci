using Dalamud.Plugin;
using LociApi.Api;
using LociApi.Helpers;
using LociApi.Ipc;

namespace Loci.Api;

public class IpcProviders : IDisposable
{
    private readonly List<IDisposable> _providers;

    private readonly EventProvider _disposedProvider;
    private readonly EventProvider _initializedProvider;

    public IpcProviders(IDalamudPluginInterface pi, ILociApi api)
    {
        // Init the independant providers
        _disposedProvider = Disposed.Provider(pi);
        _initializedProvider = Ready.Provider(pi);

        // The the rest of the providers..
        _providers =
        [
            // IpcBase
            ApiVersion.Provider(pi, api),
            IsEnabled.Provider(pi, api),
            EnabledStateChanged.Provider(pi, api),

            // IpcRegistry
            RegisterByPtr.Provider(pi, api.Registry),
            RegisterByName.Provider(pi, api.Registry),
            UnregisterByPtr.Provider(pi, api.Registry),
            UnregisterByName.Provider(pi, api.Registry),
            UnregisterAll.Provider(pi, api.Registry),
            GetHostsByPtr.Provider(pi, api.Registry),
            GetHostsByName.Provider(pi, api.Registry),
            GetHostActorCount.Provider(pi, api.Registry),
            ActorHostsChanged.Provider(pi, api.Registry),

            // IpcStatusManager
            GetManager.Provider(pi, api.StatusManager),
            GetManagerByPtr.Provider(pi, api.StatusManager),
            GetManagerByName.Provider(pi, api.StatusManager),
            GetManagerInfo.Provider(pi, api.StatusManager),
            GetManagerInfoByPtr.Provider(pi, api.StatusManager),
            GetManagerInfoByName.Provider(pi, api.StatusManager),
            SetManager.Provider(pi, api.StatusManager),
            SetManagerByPtr.Provider(pi, api.StatusManager),
            SetManagerByName.Provider(pi, api.StatusManager),
            ClearManager.Provider(pi, api.StatusManager),
            ClearManagerByPtr.Provider(pi, api.StatusManager),
            ClearManagerByName.Provider(pi, api.StatusManager),

            ManagerChanged.Provider(pi, api.StatusManager),
            ManagerStatusesChanged.Provider(pi, api.StatusManager),
            ApplyToTargetSent.Provider(pi, api.StatusManager),

            // IpcStatuses
            GetStatusInfo.Provider(pi, api.Statuses),
            GetStatusInfoList.Provider(pi, api.Statuses),
            GetStatusSummary.Provider(pi, api.Statuses),
            GetStatusSummaryList.Provider(pi, api.Statuses),
            ApplyStatus.Provider(pi, api.Statuses),
            ApplyStatuses.Provider(pi, api.Statuses),
            ApplyStatusInfo.Provider(pi, api.Statuses),
            ApplyStatusInfos.Provider(pi, api.Statuses),
            ApplyStatusByPtr.Provider(pi, api.Statuses),
            ApplyStatusesByPtr.Provider(pi, api.Statuses),
            ApplyStatusByName.Provider(pi, api.Statuses),
            ApplyStatusesByName.Provider(pi, api.Statuses),
            RemoveStatus.Provider(pi, api.Statuses),
            RemoveStatuses.Provider(pi, api.Statuses),
            RemoveStatusByPtr.Provider(pi, api.Statuses),
            RemoveStatusesByPtr.Provider(pi, api.Statuses),
            RemoveStatusByName.Provider(pi, api.Statuses),
            RemoveStatusesByName.Provider(pi, api.Statuses),
            CanLock.Provider(pi, api.Statuses),
            LockStatus.Provider(pi, api.Statuses),
            LockStatuses.Provider(pi, api.Statuses),
            UnlockStatus.Provider(pi, api.Statuses),
            UnlockStatuses.Provider(pi, api.Statuses),
            UnlockAll.Provider(pi, api.Statuses),

            StatusUpdated.Provider(pi, api.Statuses),
            ChainTriggerHit.Provider(pi, api.Statuses),

            // IpcPresets
            GetPresetInfo.Provider(pi, api.Presets),
            GetPresetInfoList.Provider(pi, api.Presets),
            GetPresetSummary.Provider(pi, api.Presets),
            GetPresetSummaryList.Provider(pi, api.Presets),
            ApplyPreset.Provider(pi, api.Presets),
            ApplyPresets.Provider(pi, api.Presets),
            ApplyPresetInfo.Provider(pi, api.Presets),
            ApplyPresetInfos.Provider(pi, api.Presets),
            ApplyPresetByPtr.Provider(pi, api.Presets),
            ApplyPresetsByPtr.Provider(pi, api.Presets),
            ApplyPresetByName.Provider(pi, api.Presets),
            ApplyPresetsByName.Provider(pi, api.Presets),
            RemovePreset.Provider(pi, api.Presets),
            RemovePresets.Provider(pi, api.Presets),
            RemovePresetByPtr.Provider(pi, api.Presets),
            RemovePresetsByPtr.Provider(pi, api.Presets),
            RemovePresetByName.Provider(pi, api.Presets),
            RemovePresetsByName.Provider(pi, api.Presets),

            PresetUpdated.Provider(pi, api.Presets),

            // IpcEvents
            GetEventList.Provider(pi, api.Events),
            GetEventInfo.Provider(pi, api.Events),
            GetEventInfoList.Provider(pi, api.Events),
            GetEventSummary.Provider(pi, api.Events),
            GetEventSummaryList.Provider(pi, api.Events),
            CreateEvent.Provider(pi, api.Events),
            DeleteEvent.Provider(pi, api.Events),
            SetEventState.Provider(pi, api.Events),
            SetEventStates.Provider(pi, api.Events),

            EventUpdated.Provider(pi, api.Events),
            EventPathMoved.Provider(pi, api.Events),
        ];
        // Indicate Loci is now ready
        _initializedProvider.Invoke();
    }

    public void Dispose()
    {
        // Dispose all providers
        foreach (var provider in _providers)
            provider.Dispose();
        _providers.Clear();
        // Dispose the initalized provider
        _initializedProvider.Dispose();
        // Let external plugins know we're disposed, then dispose the disposed provider.
        _disposedProvider.Invoke();
        _disposedProvider.Dispose();
    }
}