using Loci.Data;
using Loci.Services.Mediator;
using LociApi;
using LociApi.Api;
using LociApi.Enums;

namespace Loci.Api;
public class EventApi : DisposableMediatorSubscriberBase, ILociApiEvents
{
    private readonly ApiHelpers _helpers;
    private readonly LociManager _manager;
    private readonly LociEventData _data;

    public EventApi(ILogger<EventApi> logger, LociMediator mediator,
        ApiHelpers helpers, LociManager manager, LociEventData data)
        : base(logger, mediator)
    {
        _helpers = helpers;
        _manager = manager;
        _data = data;
        Mediator.Subscribe<LociEventChanged>(this, _ => OnEventUpdated(_.Item.GUID, _.Type is FSChangeType.Deleted));
    }

    public Dictionary<Guid, string> GetEventList()
        => LociEventData.Events.ToDictionary(e => e.GUID, e => e.Title);

    public (LociApiEc, LociEventInfo) GetEventInfo(Guid guid)
    {
        if (LociEventData.Events.FirstOrDefault(e => e.GUID == guid) is not { } lociEvent)
            return (LociApiEc.DataNotFound, default);

        return (LociApiEc.Success, lociEvent.ToTuple());
    }

    public List<LociEventInfo> GetEventInfoList()
        => [.. LociEventData.Events.Select(e => e.ToTuple())];

    public (LociApiEc, LociEventSummary) GetEventSummary(Guid guid)
    {
        if (LociEventData.Events.FirstOrDefault(e => e.GUID == guid) is not { } lociEvent)
            return (LociApiEc.DataNotFound, default);

        return (LociApiEc.Success, _helpers.ToSavedEventSummary(lociEvent));
    }

    public List<LociEventSummary> GetEventSummaryList()
        => _helpers.ToSavedEventsSummary();


    // Creates a LociEvent with the given name and type.
    // Returns the GUID of the created event, if successful.
    // eventData, the compressed form of an event can be provided, but if not required.
    public Guid CreateEvent(string eventName, string eventData, LociEventType eventType)
    {
        if (!string.IsNullOrEmpty(eventData))
        {
            // For now this does nothing
            // _data.ImportEvent();
            return Guid.Empty;
        }

        // Add as normal
        var newEvent = _data.CreateEvent(eventName);
        newEvent.EventType = eventType;
        _data.MarkEventModified(newEvent);
        return newEvent.GUID;
    }

    // Deletes an event given the provided eventId.
    // Returns Success, DataNotFound, DataInvalid.
    public LociApiEc DeleteEvent(Guid eventId)
    {
        if (LociEventData.Events.FirstOrDefault(e => e.GUID == eventId) is not { } lociEvent)
            return LociApiEc.DataNotFound;

        _data.DeleteEvent(lociEvent);
        return LociApiEc.Success;
    }

    // Sets the enabled state of an event, allowing it to be monitored for satisfying conditions.
    public LociApiEc SetEventState(Guid eventId, bool newState)
    {
        if (LociEventData.Events.FirstOrDefault(e => e.GUID == eventId) is not { } lociEvent)
            return LociApiEc.DataNotFound;

        if (lociEvent.Enabled == newState)
            return LociApiEc.NoChange;
        
        lociEvent.Enabled = newState;
        _data.MarkEventModified(lociEvent);
        return LociApiEc.Success;
    }

    public LociApiEc SetEventStates(List<Guid> eventIds, bool newState, out List<Guid> failed)
    {
        failed = [];
        var lookup = LociEventData.Events.ToDictionary(e => e.GUID, e => e);
        foreach (var id in eventIds)
        {
            if (!lookup.TryGetValue(id, out var e) || e.Enabled == newState)
            {
                failed.Add(id);
                continue;
            }
            
            e.Enabled = newState;
            _data.MarkEventModified(e);
        }

        return failed.Count == eventIds.Count 
            ? LociApiEc.DataNotFound : failed.Count is 0
                ? LociApiEc.Success : LociApiEc.NoChange;
    }

    private void OnEventUpdated(Guid id, bool wasDeleted)
        => EventUpdated?.Invoke(id, wasDeleted);
    private void OnEventPathMoved(Guid eventId, string oldPath, string newPath)
        => EventPathMoved?.Invoke(eventId, oldPath, newPath);

    public event EventUpdatedDelegate? EventUpdated;

    // Fires whenever an event moved locations in the CKFS. Provides the new path.
    // (Can revise to handle in bulk or change to be a generic action without params to act as a notifier
    public event Action<Guid, string, string>? EventPathMoved;
}
