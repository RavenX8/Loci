using CkCommons;
using CkCommons.Gui;
using CkCommons.RichText;
using CkCommons.Textures;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Loci.Data;
using LociApi.Helpers;
using LociApi.Ipc;
using OtterGui.Text;

namespace Loci.Gui;

public class IpcTesterTab : IDisposable
{
    private readonly IpcTesterRegistration _registration;
    private readonly IpcTesterStatusManagers _managers;
    private readonly IpcTesterStatuses _statuses;
    private readonly IpcTesterPresets _presets;
    private readonly IpcTesterEvents _events;

    private bool _subscribed = false;
    private (nint Addr, string Host, List<LociStatusInfo> Data) _latestApply;

    private readonly EventSubscriber<nint, string, List<LociStatusInfo>> _applyToTarget;

    public IpcTesterTab(IpcTesterRegistration registration, IpcTesterStatusManagers managers,
        IpcTesterStatuses statuses, IpcTesterPresets presets, IpcTesterEvents events)
    {
        _registration = registration;
        _managers = managers;
        _statuses = statuses;
        _presets = presets;
        _events = events;

        _applyToTarget = ApplyToTargetSent.Subscriber(Svc.PluginInterface, OnApplyToTarget);
    }

    public void Dispose()
    {
        _applyToTarget.Dispose();
    }

    private void OnApplyToTarget(nint targetPtr, string tag, List<LociStatusInfo> statuses)
        => _latestApply = (targetPtr, tag, statuses);


    private void SubscribeToIpc()
    {
        _registration.Subscribe();
        _managers.Subscribe();
        _statuses.Subscribe();
        _presets.Subscribe();
        _events.Subscribe();
        _subscribed = true;
    }

    private void UnsubscribeFromIpc()
    {
        _registration.Unsubscribe();
        _managers.Unsubscribe();
        _statuses.Unsubscribe();
        _presets.Unsubscribe();
        _events.Unsubscribe();
        _subscribed = false;
    }

    public void DrawSection()
    {
        if (CkGui.IconTextButton(FAI.Plug, "Subscribe to IPC", disabled: _subscribed))
            SubscribeToIpc();
        CkGui.AttachToolTip("THIS IS FOR TESTING PURPOSES ONLY IN THE IPC TESTER TAB." +
            "--SEP--LociIpc is already currently active and running!");
        ImGui.SameLine();
        if (CkGui.IconTextButton(FAI.PowerOff, "Unsubscribe from IPC", disabled: !_subscribed))
            UnsubscribeFromIpc();
        CkGui.AttachToolTip("THIS IS FOR TESTING PURPOSES ONLY IN THE IPC TESTER TAB." +
            "--SEP--LociIpc is already currently active and running!");

        ImGui.Separator();
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 10f);
        using var _ = ImRaii.Child("ipc-contents", ImGui.GetContentRegionAvail());
        ImGui.Text("Version");
        var (major, minor) = new ApiVersion(Svc.PluginInterface).Invoke();
        CkGui.ColorTextInline($"({major}.{minor})", ImGuiColors.DalamudYellow);

        CkGui.FrameSeparatorV();
        ImGui.Text("Enabled?:");
        var isEnabled = new IsEnabled(Svc.PluginInterface).Invoke();
        CkGui.BoolIcon(isEnabled);

        LatestTargetApply();

        _registration.Draw();
        _managers.Draw();
        _statuses.Draw();
        _presets.Draw();
        _events.Draw();

        ImGui.Separator();
        if (ImGui.CollapsingHeader("Status Managers"))
        {
            foreach (var (name, manager) in LociManager.Managers)
                DrawActorSM(name, manager);
        }
    }

    private void LatestTargetApply()
    {
        ImGui.Text("Last ApplyStatuses:");
        if (_latestApply.Addr == nint.Zero)
        {
            CkGui.ColorTextInline("None requested...", CkCol.TriStateCross.Uint());
            return;
        }

        using var ident = ImRaii.PushIndent();
        ImGui.Text("Address:");
        CkGui.ColorTextInline($"{_latestApply.Addr:X}", ImGuiColors.DalamudViolet);
        ImGui.Text("TargetHost:");
        CkGui.ColorTextInline(_latestApply.Host, ImGuiColors.DalamudViolet);
        CkGui.TextFrameAligned("Status Info:");
        ImGui.SameLine();
        using var iconGroup = ImRaii.Group();

        for (var i = 0; i < _latestApply.Data.Count; i++)
        {
            if (_latestApply.Data[i].IconID is 0)
                continue;

            LociIcon.Draw(_latestApply.Data[i].IconID, _latestApply.Data[i].Stacks, LociIcon.SizeFramed);
            Utils.AttachTooltip(_latestApply.Data[i], _latestApply.Data, []);

            if (i < _latestApply.Data.Count)
                ImUtf8.SameLineInner();
        }
    }

    private void DrawActorSM(string name, ActorSM manager)
    {
        using var _ = ImRaii.TreeNode(name);
        if (!_) return;

        ImGui.Text("Owner Valid:");
        ImGui.SameLine();
        CkGui.ColorTextBool(manager.OwnerValid ? "Valid" : "Invalid", manager.OwnerValid);

        ImGui.Text("AddTextShown:");
        CkGui.ColorTextInline(string.Join(", ", manager.AddTextShown.Select(g => g.ToString())), ImGuiColors.DalamudViolet);

        ImGui.Text("RemTextShown:");
        CkGui.ColorTextInline(string.Join(", ", manager.RemTextShown.Select(g => g.ToString())), ImGuiColors.DalamudViolet);

        ImGui.Text("Ephemeral:");
        CkGui.ColorTextInline(manager.Ephemeral.ToString(), ImGuiColors.DalamudViolet);
        if (manager.Ephemeral)
        {
            using (ImRaii.PushIndent())
            {
                foreach (var host in manager.EphemeralHosts)
                    CkGui.ColorText(host, ImGuiColors.DalamudViolet);
            }
        }

        using (var locks = ImRaii.TreeNode("Active Locks"))
        {
            if (locks)
            {
                foreach (var (id, key) in manager.LockedStatuses)
                {
                    CkGui.ColorText(id.ToString(), ImGuiColors.DalamudYellow);
                    CkGui.TextInline(" -> Locked by key");
                    CkGui.ColorTextInline($"[{key}]", ImGuiColors.DalamudViolet);
                }
            }
        }

        using (var statuses = ImRaii.TreeNode("Active Statuses"))
        {
            if (statuses)
                DrawStatuses(name, manager.Statuses);
        }
    }

    private void DrawStatuses(string id, IEnumerable<LociStatus> statuses)
    {
        using var _ = ImRaii.Table($"{id}-statuslist", 11, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit);
        if (!_) return;

        ImGui.TableSetupColumn("ID");
        ImGui.TableSetupColumn("IconID");
        ImGui.TableSetupColumn("Title");
        ImGui.TableSetupColumn("Description");
        ImGui.TableSetupColumn("VFX Path");
        ImGui.TableSetupColumn("Type");
        ImGui.TableSetupColumn("Modifiers");
        ImGui.TableSetupColumn("Stacks");
        ImGui.TableSetupColumn("Stack Steps");
        ImGui.TableSetupColumn("Chain Status");
        ImGui.TableSetupColumn("Chain Trigger");
        ImGui.TableHeadersRow();

        foreach (var status in statuses)
        {
            ImGui.TableNextColumn();
            CkGui.HoverIconText(FAI.InfoCircle, ImGuiColors.TankBlue.ToUint());
            CkGui.AttachToolTip(status.ID);
            ImGui.TableNextColumn();
            if (LociIcon.TryGetGameIcon((uint)status.IconID, false, out var wrap))
            {
                ImGui.Image(wrap.Handle, LociIcon.SizeFramed);
                CkGui.AttachToolTip($"{status.IconID}");
            }
            else
                ImGui.Text($"{status.IconID}");

            ImGui.TableNextColumn();
            CkRichText.Text(status.Title, 777);
            ImGui.TableNextColumn();
            ImGui.Dummy(new(200f, 0));
            CkRichText.Text(200f, status.Description, 777);
            ImGui.TableNextColumn();
            ImGui.Text($"{status.CustomFXPath}");
            ImGui.TableNextColumn();
            ImGui.Text($"{status.Type}");
            ImGui.TableNextColumn();
            ImGui.Text(string.Join("\n", status.Modifiers));
            ImGui.TableNextColumn();
            ImGui.Text($"{status.Stacks}");
            ImGui.TableNextColumn();
            ImGui.Text($"{status.StackSteps}");
            ImGui.TableNextColumn();
            ImGui.Text($"{status.ChainedGUID}");
            ImGui.TableNextColumn();
            ImGui.Text(status.ChainTrigger.ToString());
        }
    }

    internal static void DrawIpcRowStart(string label, string info)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(label);
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(info);
        ImGui.TableNextColumn();
    }
}
