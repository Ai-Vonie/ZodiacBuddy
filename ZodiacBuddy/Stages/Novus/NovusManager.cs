﻿using System;

using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ZodiacBuddy.BonusLight;

namespace ZodiacBuddy.Stages.Novus;

/// <summary>
/// Your buddy for the Novus stage.
/// </summary>
internal class NovusManager : IDisposable
{
    private static readonly BonusLightLevel[] BonusLightValues =
    {
        #pragma warning disable format,SA1008,SA1025
        new(  8, 4649), // Feeble
        new( 16, 4650), // Gentle
        new( 32, 4651), // Bright
        new( 48, 4652), // Brilliant
        new( 96, 4653), // Blinding
        new(128, 4654), // Newborn Star
        #pragma warning restore format,SA1008,SA1025
    };

    [Signature("40 56 48 83 EC 50 F3 0F 10 05", DetourName = nameof(AddonRelicGlassOnSetupDetour))]
    private readonly Hook<AddonRelicGlassOnSetupDelegate> addonRelicGlassOnSetupHook = null!;
    private readonly NovusWindow window;

    private DateTime dutyBeginning;
    private BonusLightDuty? dutyEntered;
    private ushort dutyIdEntered;

    /// <summary>
    /// Initializes a new instance of the <see cref="NovusManager"/> class.
    /// </summary>
    public NovusManager()
    {
        this.window = new NovusWindow();

        Service.Framework.Update += this.OnUpdate;
        Service.Toasts.QuestToast += this.OnToast;
        Service.Interface.UiBuilder.Draw += this.window.Draw;
        Service.ClientState.TerritoryChanged += this.OnTerritoryChange;

        this.OnTerritoryChange(null, Service.ClientState.TerritoryType);
        SignatureHelper.Initialise(this);
        this.addonRelicGlassOnSetupHook?.Enable();
    }

    private delegate void AddonRelicGlassOnSetupDelegate(IntPtr addon, uint a2, IntPtr a3);

    private static NovusConfiguration Configuration => Service.Configuration.Novus;

    /// <inheritdoc/>
    public void Dispose()
    {
        Service.Framework.Update -= this.OnUpdate;
        Service.Interface.UiBuilder.Draw -= this.window.Draw;
        Service.Toasts.QuestToast -= this.OnToast;
        Service.ClientState.TerritoryChanged -= this.OnTerritoryChange;

        this.addonRelicGlassOnSetupHook?.Disable();
    }

    private static bool IsInDuty()
    {
        return Service.Condition[ConditionFlag.BoundByDuty] ||
               Service.Condition[ConditionFlag.BoundByDuty56] ||
               Service.Condition[ConditionFlag.BoundByDuty95];
    }

    private void AddonRelicGlassOnSetupDetour(IntPtr addonRelicGlass, uint a2, IntPtr a3)
    {
        this.addonRelicGlassOnSetupHook.Original(addonRelicGlass, a2, a3);

        try
        {
            this.UpdateRelicGlassAddon(0, 4u);
            this.UpdateRelicGlassAddon(1, 5u);
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, $"Unhandled error during {nameof(NovusManager)}.{nameof(this.AddonRelicGlassOnSetupDetour)}");
        }
    }

    private unsafe void UpdateRelicGlassAddon(int slot, uint nodeID)
    {
        var item = Util.GetEquippedItem(slot);
        if (!NovusRelic.Items.ContainsKey(item.ItemID))
            return;

        var addon = (AtkUnitBase*)Service.GameGui.GetAddonByName("RelicGlass", 1);
        if (addon == null)
            return;

        var componentNode = (AtkComponentNode*)addon->UldManager.SearchNodeById(nodeID);
        if (componentNode == null)
            return;

        var lightText = (AtkTextNode*)componentNode->Component->UldManager.SearchNodeById(8);
        if (lightText == null)
            return;

        if (Configuration.ShowNumbersInRelicGlass)
        {
            var value = item.Spiritbond;
            lightText->SetText($"{lightText->NodeText} {value}/2000");
        }

        if (!Configuration.DontPlayRelicGlassAnimation)
            return;

        var analyzeText = (AtkTextNode*)componentNode->Component->UldManager.SearchNodeById(7);
        if (analyzeText == null)
            return;

        analyzeText->SetText(lightText->NodeText.ToString());
    }

    private void OnUpdate(Framework framework)
    {
        try
        {
            this.OnUpdateInner();
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, $"Unhandled error during {nameof(NovusManager)}.{nameof(this.OnUpdate)}");
        }
    }

    private void OnUpdateInner()
    {
        if (!Configuration.DisplayRelicInfo)
        {
            this.window.ShowWindow = false;
            return;
        }

        var mainhand = Util.GetEquippedItem(0);
        var offhand = Util.GetEquippedItem(1);

        var shouldShowWindow =
            NovusRelic.Items.ContainsKey(mainhand.ItemID) ||
            NovusRelic.Items.ContainsKey(offhand.ItemID);

        this.window.ShowWindow = shouldShowWindow;
        this.window.MainhandItem = mainhand;
        this.window.OffhandItem = offhand;
    }

    private void OnToast(ref SeString message, ref QuestToastOptions options, ref bool isHandled)
    {
        try
        {
            this.OnToastInner(ref message, ref options, ref isHandled);
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, $"Unhandled error during {nameof(NovusManager)}.{nameof(this.OnToast)}");
        }
    }

    private void OnToastInner(ref SeString message, ref QuestToastOptions options, ref bool isHandled)
    {
        if (isHandled)
            return;

        // Avoid double display if mainhand AND offhand is equipped
        if (NovusRelic.Items.ContainsKey(Util.GetEquippedItem(0).ItemID) &&
            NovusRelic.Items.TryGetValue(Util.GetEquippedItem(1).ItemID, out var relicName) &&
            message.ToString().Contains(relicName))
            return;

        foreach (var lightLevel in BonusLightValues)
        {
            if (!message.ToString().Contains(lightLevel.Message))
                continue;

            Service.Plugin.PrintMessage($"Light Intensity has increased by {lightLevel.Intensity}.");

            var territoryId = this.dutyIdEntered;
            var detectionDateTime = this.dutyBeginning;
            var territoryLight = this.dutyEntered;

            // Check territory didn't changed
            if (territoryId != Service.ClientState.TerritoryType)
                return;

            if (territoryLight == null || lightLevel.Intensity == territoryLight.DefaultLightIntensity)
                return;

            Service.BonusLightManager.AddLightBonus(territoryId, detectionDateTime, $"Light bonus detected on \"{territoryLight.DutyName}\"");
            return;
        }
    }

    private void OnTerritoryChange(object? sender, ushort territoryId)
    {
        if (!IsInDuty()) return;

        if (!BonusLightDuty.TryGetValue(territoryId, out var territoryLight))
            return;

        this.dutyBeginning = DateTime.UtcNow;
        this.dutyEntered = territoryLight;
        this.dutyIdEntered = territoryId;
    }
}
