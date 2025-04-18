using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace HP_Watcher.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration configuration; // private reference to Configuration.cs values

    public ConfigWindow(Plugin plugin) 
        : base("HP Watcher Settings###HP_Watcher_Config")
    {   
        // Configure the behavior of the window and it's size
        Flags = ImGuiWindowFlags.NoScrollbar 
      | ImGuiWindowFlags.NoScrollWithMouse 
      | ImGuiWindowFlags.NoDocking;
        
        Size = new Vector2(350, 100);
        SizeCondition = ImGuiCond.FirstUseEver; // Only use size the first time window opens

        configuration = plugin.Configuration; // reference Configuration object to edit values
    }

    public void Dispose() { }

    public override void Draw() // called every frame when config window up
    {
        int threshold = configuration.HpThresholdPercent;
        if (ImGui.InputInt("HP Threshold %", ref threshold)) // creates numeric field in config window
        {
            threshold = Math.Clamp(threshold, 1, 100); // restricts value 1-100
            configuration.HpThresholdPercent = threshold; // updates config value
            configuration.Save();
        }

        bool chatWarningEnabled = configuration.ChatWarningEnabled;
        if (ImGui.Checkbox("Chat Warning", ref chatWarningEnabled))
        {
            configuration.ChatWarningEnabled = chatWarningEnabled;
            configuration.Save();
        }

        bool soundWarningEnabled = configuration.SoundWarningEnabled;
        if (ImGui.Checkbox("Sound Warning", ref soundWarningEnabled))
        {
            configuration.SoundWarningEnabled = soundWarningEnabled;
            configuration.Save();
        }

        int soundVolumePercent = configuration.SoundVolumePercent;
        if (ImGui.SliderInt("Sound Volume", ref soundVolumePercent, 0, 200, "%d%%"))
        {
            configuration.SoundVolumePercent = soundVolumePercent;
            configuration.Save();
        }
    }
}