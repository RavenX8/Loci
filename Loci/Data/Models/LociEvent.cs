using LociApi.Enums;
using MemoryPack;

namespace Loci.Data;

// A very WIP structure for events.
[Serializable]
[MemoryPackable]
public partial class LociEvent
{
    internal string ID => GUID.ToString();

    // Essential
    public const int Version = 1;
    public Guid GUID = Guid.NewGuid();
    bool Enabled = false;
    public string Title = string.Empty;
    public string Description = string.Empty;
    public LociEventType EventType;


    // Used for timers? (WIP)
    [MemoryPackIgnore] public int Days = 0;
    [MemoryPackIgnore] public int Hours = 0;
    [MemoryPackIgnore] public int Minutes = 0;
    [MemoryPackIgnore] public int Seconds = 0;
    [MemoryPackIgnore] public bool NoExpire = false;
    public bool ShouldSerializeGUID() => GUID != Guid.Empty;
    public bool IsNull() => Description is null || Title is null;

    public LociEventInfo ToTuple()
        => new LociEventInfo
        {
            Version = Version,
            GUID = GUID,
            Enabled = Enabled,
            Title = Title,
            Description = Description,
            EventType = EventType,
        };

    public static LociEvent FromTuple(LociEventInfo eventInfo)
    {
        return new LociEvent
        {
            GUID = eventInfo.GUID,
            Enabled = eventInfo.Enabled,
            Title = eventInfo.Title,
            Description = eventInfo.Description,
            EventType = eventInfo.EventType,
        };
    }

    public string ReportString()
        => $"[LociStatus: GUID={GUID}," +
        $"\nEnabled={Enabled}" +
        $"\nTitle={Title}" +
        $"\nDescription={Description}" +
        $"\nEventType={EventType}";
}
