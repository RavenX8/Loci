using Loci.Data;
using Loci.DrawSystem;
using LociApi.Enums;
using System.Runtime.CompilerServices;

namespace Loci.Api;

// Commonly shared helper logic.
// Maybe make it more of a helper later
public class ApiHelpers(LociManager manager, StatusesFS statusFS, PresetsFS presetFS, LociEventsFS eventsFS)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public string ToLociName(string charaName, string buddyName)
        => buddyName.Length > 0 ? $"{charaName}s {buddyName}" : charaName;

    public LociApiEc AddEphemeralHost(ActorSM sm, string identifier)
    {
        if (LociManager.ClientSM == sm)
            return LociApiEc.ClientForbidden;

        return sm.EphemeralHosts.Add(identifier) ? LociApiEc.Success : LociApiEc.NoChange;
    }

    public LociApiEc RemoveEphemeralHost(ActorSM sm, string identifier)
    {
        if (LociManager.ClientSM == sm)
            return LociApiEc.ClientForbidden;

        return sm.EphemeralHosts.Remove(identifier) ? LociApiEc.Success : LociApiEc.NoChange;
    }

    public LociStatusSummary ToSavedStatusSummary(LociStatus s)
    {
        var foundPath = statusFS.FindLeaf(s, out var path) ? path.FullName() : string.Empty;
        return new LociStatusSummary(s.GUID, foundPath, s.IconID, s.Title, s.Description);
    }

    public List<LociStatusSummary> ToSavedStatusesSummary()
    {
        var ret = new List<LociStatusSummary>(LociData.Statuses.Count);
        foreach (var s in LociData.Statuses)
        {
            var foundPath = statusFS.FindLeaf(s, out var path) ? path.FullName() : string.Empty;
            ret.Add((s.GUID, foundPath, s.IconID, s.Title, s.Description));
        }
        return ret;
    }

    // More efficient to do this call in bulk than single, but still quite fast.
    public LociPresetSummary ToSavedPresetSummary(LociPreset p)
    {
        var lookup = LociData.Statuses.ToDictionary(s => s.GUID, s => s);
        var foundPath = presetFS.FindLeaf(p, out var path) ? path.FullName() : string.Empty;
        var icons = p.Statuses.Select(sid => lookup.TryGetValue(sid, out var s) ? s.IconID : 0).ToList();
        return new LociPresetSummary(p.GUID, foundPath, icons, p.Title, p.Description);
    }

    public List<LociPresetSummary> ToSavedPresetsSummary()
    {
        var lookup = LociData.Statuses.ToDictionary(s => s.GUID, s => s);
        var ret = new List<LociPresetSummary>(LociData.Presets.Count);
        foreach (var p in LociData.Presets)
        {
            var foundPath = presetFS.FindLeaf(p, out var path) ? path.FullName() : string.Empty;
            var icons = p.Statuses.Select(sid => lookup.TryGetValue(sid, out var s) ? s.IconID : 0).ToList();
            ret.Add((p.GUID, foundPath, icons, p.Title, p.Description));
        }
        return ret;
    }

    public LociEventSummary ToSavedEventSummary(LociEvent e)
    {
        var foundPath = eventsFS.FindLeaf(e, out var path) ? path.FullName() : string.Empty;
        return new LociEventSummary(e.GUID, foundPath, e.Enabled, e.EventType, e.Title, e.Description);
    }

    public List<LociEventSummary> ToSavedEventsSummary()
    {
        var ret = new List<LociEventSummary>(LociEventData.Events.Count);
        foreach (var e in LociEventData.Events)
        {
            var foundPath = eventsFS.FindLeaf(e, out var path) ? path.FullName() : string.Empty;
            ret.Add((e.GUID, foundPath, e.Enabled, e.EventType, e.Title, e.Description));
        }
        return ret;
    }
}

