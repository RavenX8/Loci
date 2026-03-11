using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Loci.Data;
using Loci.DrawSystem;
using Loci.Services;
using Loci.Services.Mediator;

namespace Loci.Gui;

public class SettingsTab
{
    private readonly ILogger<SettingsTab> _logger;
    private readonly LociMediator _mediator;
    private readonly MainConfig _config;
    private readonly LociData _data;
    private readonly LociManager _manager;
    private readonly StatusesFS _statusFileSystem;
    private readonly PresetsFS _presetFileSystem;

    public SettingsTab(ILogger<SettingsTab> logger, LociMediator mediator, MainConfig config, 
        LociData data, LociManager manager, StatusesFS statusFS, PresetsFS presetFS)
    {
        _logger = logger;
        _mediator = mediator;
        _config = config;
        _data = data;
        _manager = manager;
        _statusFileSystem = statusFS;
        _presetFileSystem = presetFS;
    }

    public unsafe void DrawSettings()
    {
        CkGui.FontText("Functionality", Fonts.Default150Percent);
        var enabled = _config.Current.Enabled;
        if (ImGui.Checkbox($"Enable Module", ref enabled))
        {
            _config.Current.Enabled = enabled;
            _config.Save();
            _mediator.Publish(new NewEnabledStateMessage(enabled));
        }
        DrawIndentedEnables();

        CkGui.FontText("Limiters", Fonts.Default150Percent);
        var offInDuty = _config.Current.OffInDuty;
        if (ImGui.Checkbox("Disable in Duties/Instances", ref offInDuty))
        {
            _config.Current.OffInDuty = offInDuty;
            _config.Save();
        }

        var offInCombat = _config.Current.OffInCombat;
        if (ImGui.Checkbox("Disable in Combat", ref offInCombat))
        {
            _config.Current.OffInCombat = offInCombat;
            _config.Save();
        }

        var canEsuna = _config.Current.AllowEsuna;
        if (ImGui.Checkbox("Allow esunable statuses", ref canEsuna))
        {
            _config.Current.AllowEsuna = canEsuna;
            _config.Save();
        }

        var othersCanEsuna = _config.Current.OthersCanEsuna;
        if (ImGui.Checkbox("Others can Esuna your statuses", ref othersCanEsuna))
        {
            _config.Current.OthersCanEsuna = othersCanEsuna;
            _config.Save();
        }

        DrawMigrate();
    }

    private void DrawIndentedEnables()
    {
        using var dis = ImRaii.Disabled(!_config.Current.Enabled);
        using var indent = ImRaii.PushIndent();

        var vfxOn = _config.Current.SheVfxEnabled;
        var vfxLimited = _config.Current.SheVfxRestricted;
        var flyTextOn = _config.Current.FlyText;
        var flyTextLimit = _config.Current.FlyTextLimit;

        if (ImGui.Checkbox($"Loci VFX", ref vfxOn))
        {
            _config.Current.SheVfxEnabled = vfxOn;
            _config.Save();
        }
        CkGui.AttachToolTip("If VFX are applied on Loci Status application");

        if (ImGui.Checkbox($"Restrict VFX", ref vfxLimited))
        {
            _config.Current.SheVfxRestricted = vfxLimited;
            _config.Save();
        }
        CkGui.AttachToolTip("Restricts Vfx to only friends, party and nearby actors");

        if (ImGui.Checkbox($"Fly/Popup Text", ref flyTextOn))
        {
            _config.Current.FlyText = flyTextOn;
            _config.Save();
        }

        ImGui.SetNextItemWidth(150f);
        if (ImGui.SliderInt("Fly/Popup Text Limit", ref flyTextLimit, 5, 20))
        {
            _config.Current.FlyTextLimit = flyTextLimit;
            _config.Save();
        }
        CkGui.AttachToolTip("How many Fly/Popup Texts can be active simultaneously.");
    }

    private void DrawMigrate()
    {
        var oldDirExists = Directory.Exists(GetOldMigratableDirectoryPath());
        var sundDirExists = Directory.Exists(GetSundMigratableDirectoryPath());
        if (!oldDirExists && !sundDirExists)
            return;

        ImGui.Separator();
        CkGui.FontText("Data Migration", Fonts.Default150Percent);
        var shiftAndCtrlPressed = ImGui.GetIO().KeyShift && ImGui.GetIO().KeyCtrl;

        if (oldDirExists)
        {
            if (CkGui.IconTextButton(FAI.FileImport, "Import Statuses (From Moodles)", disabled: !shiftAndCtrlPressed))
            {
                var statusFS = GetOldMigrationFilePath("MoodleFileSystem.json");
                var statuses = GetOldMigrationFilePath("DefaultConfig.json");
                if (File.Exists(statusFS) && File.Exists(statuses))
                {
                    _logger.LogInformation($"Migrating from {statusFS}");
                    try
                    {
                        var defaultJson = JObject.Parse(File.ReadAllText(statuses));
                        _data.MoodleStatusMigration(defaultJson);
                        _statusFileSystem.MergeWithMigratableFile(statusFS);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to migrate statuses from {statuses}");
                    }
                }
            }
            CkGui.AttachToolTip("Migrate all statuses to Loci.--SEP----COL--Must hold CTRL+SHIFT to execute.--COL--", ImGuiColors.DalamudOrange);

            if (CkGui.IconTextButton(FAI.FileImport, "Import Presets (From Moodles)", disabled: !shiftAndCtrlPressed))
            {
                var presetFS = GetOldMigrationFilePath("PresetFileSystem.json");
                var presets = GetOldMigrationFilePath("DefaultConfig.json");
                if (File.Exists(presetFS) && File.Exists(presets))
                {
                    _logger.LogInformation($"Migrating from {presetFS}");
                    try
                    {
                        var defaultJson = JObject.Parse(File.ReadAllText(presets));
                        _data.MoodlePresetMigration(defaultJson);
                        // Then update the FS.
                        _presetFileSystem.MergeWithMigratableFile(presetFS);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to migrate presets from {presets}");
                    }
                }
            }
            CkGui.AttachToolTip("Migrate all presets to Loci.--SEP----COL--Must hold CTRL+SHIFT to execute.--COL--", ImGuiColors.DalamudOrange);
            ImGui.Separator();
        }

        if (sundDirExists)
        {
            if (CkGui.IconTextButton(FAI.FileImport, "Migrate Statuses (From Sundouleia)", disabled: !shiftAndCtrlPressed))
            {
                var statusFS = Path.Combine(GetSundMigratableDirectoryPath(), "filesystem", "fs-statuses.json");
                var statuses = Path.Combine(GetSundMigratableDirectoryPath(), "lociData.json");
                if (File.Exists(statusFS) && File.Exists(statuses))
                {
                    _logger.LogInformation($"Migrating from {statusFS}");
                    try
                    {
                        var defaultJson = JObject.Parse(File.ReadAllText(statuses));
                        _data.SundStatusMigration(defaultJson);
                        _statusFileSystem.MergeWithMigratableFile(statusFS);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to migrate statuses from {statuses}");
                    }
                }
            }
            CkGui.AttachToolTip("Migrate all statuses to Sundouleia.--SEP----COL--Must hold CTRL+SHIFT to execute.--COL--", ImGuiColors.DalamudOrange);

            if (CkGui.IconTextButton(FAI.FileImport, "Migrate Presets (From Sundouleia)", disabled: !shiftAndCtrlPressed))
            {
                try
                {
                    var presetFS = Path.Combine(GetSundMigratableDirectoryPath(), "filesystem", "fs-presets.json");
                    var presets = Path.Combine(GetSundMigratableDirectoryPath(), "lociData.json");
                    if (File.Exists(presetFS) && File.Exists(presets))
                    {
                        _logger.LogInformation($"Migrating from {presetFS}");
                        var defaultJson = JObject.Parse(File.ReadAllText(presets));
                        _data.SundPresetMigration(defaultJson);
                        _presetFileSystem.MergeWithMigratableFile(presetFS);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to migrate presets");
                }
            }
            CkGui.AttachToolTip("Migrate all presets to Sundouleia.--SEP----COL--Must hold CTRL+SHIFT to execute.--COL--", ImGuiColors.DalamudOrange);
        }
    }

    #region Helpers
    // Locate if we are able to migrate
    private string GetOldMigratableDirectoryPath()
        => Path.GetDirectoryName(FileProvider.Directory) is { } path ? Path.Combine(path, "Moodles") : string.Empty;
    
    private string GetSundMigratableDirectoryPath()
         => Path.GetDirectoryName(FileProvider.Directory) is { } path ? Path.Combine(path, "Sundouleia") : string.Empty;

    private string GetOldMigrationFilePath(string fileName)
        => Path.Combine(GetOldMigratableDirectoryPath(), fileName);
    #endregion Helpers
}
