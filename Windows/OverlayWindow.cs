using System;
using System.Configuration;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace HP_Watcher.Windows;
public class OverlayWindow : Window
{
    private Configuration config;

    public OverlayWindow(Configuration config) 
        : base("###HP_Watcher_Overlay", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoBackground |
                                        ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoScrollbar |
                                        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove)
    {
        this.config = config;
        IsOpen = true;
        RespectCloseHotkey = false;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (!config.ThresholdAlerts.HighlightEnabled)
            return;

        ImGui.SetNextWindowPos(new Vector2(10, 300), ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(300, 300), ImGuiCond.Always);
        ImGui.Begin("###HPOverlayWindow", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs);

        // Show for yourself
        var player = Plugin.ClientState.LocalPlayer;
        if (player != null)
        {
            float hpPercent = (float)player.CurrentHp / player.MaxHp;
            if (hpPercent < config.ThresholdRatio)
            {
                var color = config.ThresholdAlerts.HighlightColor.ToLower() == "blue"
                    ? new Vector4(0f, 0.4f, 1f, 0.4f)
                    : new Vector4(1f, 0f, 0f, 0.4f);

                ImGui.PushStyleColor(ImGuiCol.Button, color);
                ImGui.Button($"{player.Name.TextValue} LOW HP!");
                ImGui.PopStyleColor();
            }
        }

        // Show for party members
        foreach (var member in Plugin.PartyList)
        {
            float partyHpPercent = (float)member.CurrentHP / member.MaxHP;
            if (partyHpPercent < config.ThresholdRatio)
            {
                var color = config.ThresholdAlerts.HighlightColor.ToLower() == "blue"
                    ? new Vector4(0f, 0.4f, 1f, 0.4f)
                    : new Vector4(1f, 0f, 0f, 0.4f);

                ImGui.PushStyleColor(ImGuiCol.Button, color);
                ImGui.Button($"{member.Name.TextValue} LOW HP!");
                ImGui.PopStyleColor();
            }
        }

        ImGui.End();
    }
}