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
    }

    public override void Draw()
    {
        if (!config.ThresholdAlerts.HighlightEnabled) return;

        foreach (var member in Plugin.PartyList)
        {
            // same drawing logic here
        }
    }
}
