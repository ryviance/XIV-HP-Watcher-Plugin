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
        ImGui.Text("General Settings");

        int soundVolumePercent = configuration.SoundVolumePercent;
        if (ImGui.SliderInt("Alert Sound Volume", ref soundVolumePercent, 0, 200, "%d%%"))
        {
            configuration.SoundVolumePercent = soundVolumePercent;
            configuration.Save();
        }

        ImGui.Spacing();
        ImGui.Dummy(new Vector2(0, 10));

        if (ImGui.CollapsingHeader("HP Threshold Alert Settings"))
        {  
            int threshold = configuration.HpThresholdPercent;
            if (ImGui.InputInt("HP Threshold %", ref threshold)) // creates numeric field in config window
            {
                threshold = Math.Clamp(threshold, 1, 100); // restricts value 1-100
                configuration.HpThresholdPercent = threshold; // updates config value
                configuration.Save();
            }

            bool chatWarningEnabled = configuration.ThresholdAlerts.ChatEnabled;
            if (ImGui.Checkbox("Chat Warning", ref chatWarningEnabled))
            {
                configuration.ThresholdAlerts.ChatEnabled = chatWarningEnabled;
                configuration.Save();
            }

            bool soundWarningEnabled = configuration.ThresholdAlerts.SoundEnabled;
            if (ImGui.Checkbox("Sound Warning", ref soundWarningEnabled))
            {
                configuration.ThresholdAlerts.SoundEnabled = soundWarningEnabled;
                configuration.Save();
            }

            bool highlightWarningEnabled = configuration.ThresholdAlerts.HighlightEnabled;
            if (ImGui.Checkbox("Highlight Warning", ref highlightWarningEnabled))
            {
                configuration.ThresholdAlerts.HighlightEnabled = highlightWarningEnabled;
                configuration.Save();
            }

            string[] colorOptions = new[] { "Red", "Blue", "Cyan", "Green" };
            // Get the index of the current config color (fallback to 0 if not found)
            int selectedIndex = Array.FindIndex(colorOptions, c => string.Equals(c, configuration.ThresholdAlerts.HighlightColor, StringComparison.OrdinalIgnoreCase));
            if (selectedIndex < 0)
                selectedIndex = 0; // default fallback
            if (ImGui.Combo("Highlight Color", ref selectedIndex, colorOptions, colorOptions.Length))
            {
                configuration.ThresholdAlerts.HighlightColor = colorOptions[selectedIndex];
                configuration.Save();
            }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Lethal Damage Alert Settings"))
        {  
        }
    }
}