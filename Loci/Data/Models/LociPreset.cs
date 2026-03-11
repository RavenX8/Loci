namespace Loci.Data;

[Serializable]
public class LociPreset
{
    internal string ID => GUID.ToString();

    public const int Version = 1;
    public Guid GUID = Guid.NewGuid();
    
    public List<Guid> Statuses = [];
    public PresetApplyType ApplyType = PresetApplyType.UpdateExisting;

    public string Title = string.Empty;
    public string Description = string.Empty;

    public bool ShouldSerializeGUID()
        => GUID != Guid.Empty;

    public LociPresetInfo ToTuple()
        => (Version, GUID, Statuses, (byte)ApplyType, Title, Description);
}
