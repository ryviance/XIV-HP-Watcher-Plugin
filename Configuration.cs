// File Name: Configuration.cs
// Descriptions: Config variables, which affect how the plugin work

using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace HP_Watcher;

[Serializable]
public class Configuration : IPluginConfiguration 
{
    public int Version { get; set; } = 1;
    public int HpThresholdPercent { get; set; } = 60;
    public void Save()  
    {   
        // Method description: Saves user config settings
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}