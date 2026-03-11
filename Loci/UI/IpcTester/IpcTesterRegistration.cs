using CkCommons;
using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Loci.Services;
using LociApi.Enums;
using LociApi.Helpers;
using LociApi.Ipc;
using System.Globalization;

namespace Loci.Gui;

public class IpcTesterRegistration : IIpcTesterGroup
{
    private readonly EventSubscriber<nint, string> _actorHostsChanged;

    private string _actorAddrString = string.Empty;
    private nint _actorAddr = nint.Zero;
    private string _nameToProcess = string.Empty;
    private string _tagToBind = string.Empty;

    private string _lastRegisteredActor = string.Empty;
    private string _lastUnregisteredActor = string.Empty;
    private string _lastRegisteredCode = string.Empty;
    private string _lastUnregisteredCode = string.Empty;
    private LociApiEc _lastReturnCode = LociApiEc.UnkError;

    private List<string> _identifiedHosts = [];
    private int _hostCountForLabel = 0;

    private (nint ActorPtr, string HostTag) _lastActorHostsChange;

    public IpcTesterRegistration()
    {
        _actorHostsChanged = ActorHostsChanged.Subscriber(Svc.PluginInterface, OnActorHostsChanged);
    }

    public bool IsSubscribed { get; private set; } = false;

    public void Subscribe()
    {
        _actorHostsChanged.Enable();
        IsSubscribed = true;
    }

    public void Unsubscribe()
    {
        _actorHostsChanged.Disable();
        IsSubscribed = false;
    }

    public void Dispose()
        => Unsubscribe();

    private void OnActorHostsChanged(nint actorPtr, string hostTag)
        => _lastActorHostsChange = (actorPtr, hostTag);

    public unsafe void Draw()
    {
        if (ImGui.InputTextWithHint("##drawObject", "Player Address..", ref _actorAddrString, 16, ImGuiInputTextFlags.CharsHexadecimal))
            _actorAddr = nint.TryParse(_actorAddrString, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var tmp) ? tmp : nint.Zero;
        ImGui.InputTextWithHint("##actorName", "Player Name@World...", ref _nameToProcess, 100);
        ImGui.InputTextWithHint("##binding-tag", "HostTagToAssign", ref _tagToBind, 60);

        using var table = ImRaii.Table(string.Empty, 4, ImGuiTableFlags.SizingFixedFit);
        if (!table) return;

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TextUnformatted("Last Return Code");
        ImGui.TableNextColumn();
        CkGui.ColorText(_lastReturnCode.ToString(), ImGuiColors.DalamudYellow);

        IpcTesterUI.DrawIpcRowStart(RegisterByPtr.Label, "Register w/ Address");
        if (CkGui.SmallIconTextButton(FAI.Share, "Register", disabled: !IsSubscribed || _actorAddr == nint.Zero))
        {
            _lastReturnCode = new RegisterByPtr(Svc.PluginInterface).Invoke(_actorAddr, _tagToBind);
            if (_lastReturnCode is LociApiEc.Success)
            {
                // Attempt to fetch the chara.
                if (CharaWatcher.TryGetValue(_actorAddr, out Character* chara))
                {
                    _lastRegisteredActor = $"{chara->GetNameWithWorld()} ({(nint)chara})";
                    _lastRegisteredCode = _tagToBind;
                }
            }
        }

        IpcTesterUI.DrawIpcRowStart(RegisterByName.Label, "PlayerName@World / PlayerNames Pet Name");
        if (CkGui.SmallIconTextButton(FAI.Share, "Register", disabled: !IsSubscribed || _nameToProcess.Length > 0))
        {
            _lastReturnCode = new RegisterByName(Svc.PluginInterface).Invoke(_nameToProcess, _tagToBind);
            if (_lastReturnCode is LociApiEc.Success)
            {
                _lastRegisteredActor = _nameToProcess;
                _lastRegisteredCode = _tagToBind;
            }
        }

        IpcTesterUI.DrawIpcRowStart(UnregisterByPtr.Label, "Unregister w/ Address");
        if (CkGui.SmallIconTextButton(FAI.Share, "Unregister", disabled: !IsSubscribed || _actorAddr == nint.Zero))
        {
            _lastReturnCode = new UnregisterByPtr(Svc.PluginInterface).Invoke(_actorAddr, _tagToBind);
            if (_lastReturnCode is LociApiEc.Success)
            {
                // Attempt to fetch the chara.
                if (CharaWatcher.TryGetValue(_actorAddr, out Character* chara))
                {
                    _lastUnregisteredActor = $"{chara->GetNameWithWorld()} ({(nint)chara})";
                    _lastUnregisteredCode = _tagToBind;
                }
            }
        }

        IpcTesterUI.DrawIpcRowStart(UnregisterByName.Label, "PlayerName@World / PlayerNames Pet Name");
        if (CkGui.SmallIconTextButton(FAI.Share, "Unregister", disabled: !IsSubscribed || _nameToProcess.Length > 0))
        {
            _lastReturnCode = new UnregisterByName(Svc.PluginInterface).Invoke(_nameToProcess, _tagToBind);
            if (_lastReturnCode is LociApiEc.Success)
            {
                _lastUnregisteredActor = _nameToProcess;
                _lastUnregisteredCode = _tagToBind;
            }
        }

        IpcTesterUI.DrawIpcRowStart(UnregisterAll.Label, "Unregister all for HostTag");
        if (CkGui.SmallIconTextButton(FAI.Share, "Unregister All", disabled: !IsSubscribed || _tagToBind.Length == 0))
        {
            _hostCountForLabel = new UnregisterAll(Svc.PluginInterface).Invoke(_tagToBind);
        }

        IpcTesterUI.DrawIpcRowStart(GetHostsByPtr.Label, "Get Hosts w/ Address");
        if (CkGui.SmallIconTextButton(FAI.Download, "Get Hosts", disabled: !IsSubscribed || _actorAddr == nint.Zero))
            _identifiedHosts = new GetHostsByPtr(Svc.PluginInterface).Invoke(_actorAddr);

        IpcTesterUI.DrawIpcRowStart(GetHostsByName.Label, "Get Hosts w/ Name");
        if (CkGui.SmallIconTextButton(FAI.Download, "Get Hosts", disabled: !IsSubscribed || _nameToProcess.Length == 0))
            _identifiedHosts = new GetHostsByName(Svc.PluginInterface).Invoke(_nameToProcess);

        IpcTesterUI.DrawIpcRowStart(GetHostActorCount.Label, "Count Actors for Host");
        if (CkGui.SmallIconTextButton(FAI.Download, "Get Count", disabled: !IsSubscribed || _tagToBind.Length == 0))
            _hostCountForLabel = new GetHostActorCount(Svc.PluginInterface).Invoke(_tagToBind);
    }
}
