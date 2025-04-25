// File Name: Configuration.cs
// Descriptions: Config variables, which affect how the plugin work

using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Numerics;

namespace HP_Watcher;

[Serializable]
public class AlertSettings {
    public bool ChatEnabled { get; set; } = true;
    public bool SoundEnabled { get; set; } = true;
    public bool HighlightEnabled { get; set; } = true;
    public string HighlightColor {get; set; } = "red";
}

[Serializable]
public class Configuration : IPluginConfiguration 
{
    public int Version { get; set; } = 1;
    public int HpThresholdPercent { get; set; } = 60;
    // Variable used so we dont have to convert HPThresholdPercent to float every frame in Plugin.cs
    public float ThresholdRatio => HpThresholdPercent / 100f;
    public int SoundVolumePercent { get; set; } = 100;
    public AlertSettings ThresholdAlerts { get; set; } = new();
    public AlertSettings IncomingDamageAlerts { get; set; } = new();

    // Overlay settings
    public Vector2 OverlayPosition = new Vector2(200, 200);
    public Vector2 OverlaySize = new Vector2(200, 20);
    public bool OverlayLocked = false;


    public void Save()  
    {   
        // Method description: Saves user config settings
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}