using CkCommons.Gui;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Loci.Data;
using Loci.Services.Mediator;

namespace Loci.Gui.Components;

public class IpcTesterTabs : IconTextTabBar<IpcTesterTabs.SelectedTab>
{
    public enum SelectedTab
    {
        Registry,
        Statuses,
        Presets,
        StatusManagers,
        Events,
    }

    public override SelectedTab TabSelection
    {
        get => base.TabSelection;
        set
        {
            _config.Current.IpcTab = value;
            _config.Save();
            base.TabSelection = value;
        }
    }

    private readonly MainConfig _config;
    public IpcTesterTabs(MainConfig config, LociMediator mediator)
    {
        _config = config;
        TabSelection = _config.Current.IpcTab;

        AddDrawButton(FAI.Scroll, "Registry", SelectedTab.Registry, "IPC related to Ephemeral Host Control");
        AddDrawButton(FAI.TheaterMasks, "Statuses", SelectedTab.Statuses, "IPC related to Statuses");
        AddDrawButton(FAI.LayerGroup, "Presets", SelectedTab.Presets, "IPC related to Presets");
        AddDrawButton(FAI.Wrench, "Managers", SelectedTab.StatusManagers, "IPC related to StatusManagers");
        AddDrawButton(FAI.Exclamation, "LociEvents", SelectedTab.Events, "IPC related to LociEvents");

        TabSelectionChanged += (oldTab, newTab) => mediator.Publish(new IpcTabBarChangedMessage(newTab));
    }

    public override void Draw(float availableWidth)
    {
        if (_tabButtons.Count == 0)
            return;

        using var color = ImRaii.PushColor(ImGuiCol.Button, ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, 0)));
        var spacing = ImGui.GetStyle().ItemSpacing;
        var buttonX = (availableWidth - (spacing.X * (_tabButtons.Count - 1))) / _tabButtons.Count;
        var buttonY = CkGui.IconButtonSize(FontAwesomeIcon.Pause).Y;
        var buttonSize = new Vector2(buttonX, buttonY);
        var drawList = ImGui.GetWindowDrawList();
        var underlineColor = ImGui.GetColorU32(ImGuiCol.Separator);

        foreach (var tab in _tabButtons)
            DrawTabButton(tab, buttonSize, spacing, drawList);

        // advance to the new line and dispose of the button color.
        ImGui.NewLine();
        color.Dispose();

        ImGuiHelpers.ScaledDummy(3f);
        ImGui.Separator();
    }
}
