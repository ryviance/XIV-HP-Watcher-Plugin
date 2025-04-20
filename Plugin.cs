// File: Plugin.cs
// Description: Main plugin logic

using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Windowing;
using HP_Watcher.Windows;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading;
using System.Threading.Tasks;

namespace HP_Watcher;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!; // For UI and loading files
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!; // To create slash commands
    [PluginService] internal static IClientState ClientState { get; private set; } = null!; // To access any player-related data
    [PluginService] internal static IPluginLog Log { get; private set; } = null!; // For development/bug reports
    [PluginService] internal static IChatGui Chat { get; private set; } = null!;  // For system messages in game from the plugin
    [PluginService] internal static IPartyList PartyList { get; private set; } = null!; // For party list functionality

    // Constants for command strings
    private const string HpConfigCommand = "/hpconfig";
    private const string PartyHpCommand = "/php";

    // Variables for method utilities
    private readonly Dictionary<string, bool> lowHpWarnings = new(); // Used in CheckHP to not spam warnings
    private DateTime lastCleanupTime = DateTime.Now; // Keep track of time to purge readonly dictionary of unused keys

    public Configuration Configuration { get; init; } // Initialize config data

    // ImGUI windows
    public readonly WindowSystem WindowSystem = new("HP_Watcher");
    private ConfigWindow ConfigWindow { get; init; }
    private OverlayWindow OverlayWindow { get; init; }

    private CancellationTokenSource? cleanupTaskToken; // Threading for cleanup

    public Plugin()
    {   
        // Initialize and load previous config data from Configuration.cs
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration(); 
        
        // Initialize and draw ImGUI config window
        ConfigWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(ConfigWindow);

        OverlayWindow = new OverlayWindow(Configuration);
        WindowSystem.AddWindow(OverlayWindow);


        // UIBuilder.Draw is a hook for every-frame logic, not exclusively used for UI:
        PluginInterface.UiBuilder.Draw += CheckHp; // Check HP of party members and player every frame
        PluginInterface.UiBuilder.Draw += DrawUI; // Check every frame if it needs to draw config window (due to button/command, etc.)
        
        // Clean HP Warning dictionary using threading
        cleanupTaskToken = new CancellationTokenSource();
        _ = RunPeriodicCleanup(cleanupTaskToken.Token); // Wait every 15 minutes to clean

        // Add commands and their help messages
        CommandManager.AddHandler(HpConfigCommand, new CommandInfo(OnHpConfigCommand)
        {
            HelpMessage = "Opens HP Watcher configuration window."
        });

        CommandManager.AddHandler(PartyHpCommand, new CommandInfo(OnPartyHpCommand)
        {
            HelpMessage = "Displays HP of self and party members."
        });

        // Add functionality to "Open" button in the plugin installer
        PluginInterface.UiBuilder.OpenMainUi += ToggleConfigUI;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

        // Startup log message in Dalamud's plugin log
        Log.Information($"{PluginInterface.Manifest.Name} loaded. Ready to watch your HP.");
    }

    public void Dispose()
    {   
        WindowSystem.RemoveAllWindows(); // Clear memory related to all ImGUI windows
        ConfigWindow.Dispose(); // Clear config window memory
        cleanupTaskToken?.Cancel(); // Stop cleaning dictionary on shutdown
        PluginInterface.UiBuilder.Draw -= CheckHp; // Stop checking HP 
        PluginInterface.UiBuilder.Draw -= DrawUI; // Stop checking to draw UI 

        // Dispose commands
        CommandManager.RemoveHandler(PartyHpCommand);   
        CommandManager.RemoveHandler(HpConfigCommand);   
    }

    private void OnPartyHpCommand(string command, string args)
    {   
        // Method description: Displays HP of player and all party members in chat after typing /php.
        // Show self HP
        var player = ClientState.LocalPlayer;
        if (player != null){
            Chat.Print($"{player.Name.TextValue} - {player.CurrentHp}/{player.MaxHp} HP (You)");
        }

        // If in party show everyone else's hp
        if (PartyList.Length > 0)
        {
            foreach (var member in PartyList)
            {
                if (member.Name.TextValue == player?.Name.TextValue && member.World.RowId == player?.HomeWorld.RowId) // Skip self
                {
                    continue;
                }
                Chat.Print($"{member.Name} - {member.CurrentHP}/{member.MaxHP} HP");
            }
        }
    }

    private void OnHpConfigCommand(string command, string args)
    {
        // Method description: Opens plugin configuration window
        ToggleConfigUI();
    }

    private void CheckHp()
    {   
        // Description: Check player and party member every frame for HP threshold + warning
        float threshold = Configuration.ThresholdRatio;
        bool chatWarning = Configuration.ThresholdAlerts.ChatEnabled;
        bool soundWarning = Configuration.ThresholdAlerts.SoundEnabled;
        string soundPath = Path.Combine(PluginInterface.AssemblyLocation.Directory!.FullName, "Data", "critical-health-pokemon.wav");
        float volume = Configuration.SoundVolumePercent;

        var player = ClientState.LocalPlayer;
        if (player != null)
            CheckAndWarn(player.Name.TextValue, player.CurrentHp, player.MaxHp, threshold, chatWarning, soundWarning, soundPath, volume, isSelf: true);

        foreach (var member in PartyList)
        {
            if (player != null &&
                member.Name == player.Name &&
                member.World.RowId == player.HomeWorld.RowId)
                continue;

            CheckAndWarn(member.Name.TextValue, member.CurrentHP, member.MaxHP, threshold, chatWarning, soundWarning, soundPath, volume, isSelf: false);
        }
    }

    private void CheckAndWarn(string playerName, uint currentHp, uint maxHp, float threshold, bool chatWarning, bool soundWarning, string soundPath, float volume, bool isSelf)
    {   
        // Description: Helper method for CheckHp(), checks conditions and sends warnings
        float hpPercent = (float)currentHp / maxHp;

        if (hpPercent >= threshold)
        {
            lowHpWarnings[playerName] = false;
            return;
        }

        if (lowHpWarnings.GetValueOrDefault(playerName, false))
            return;

        if (chatWarning)
        {
            var message = isSelf
                ? $"You are below {Configuration.HpThresholdPercent}% HP! ({currentHp}/{maxHp})"
                : $"{playerName} is below {Configuration.HpThresholdPercent}% HP! ({currentHp}/{maxHp})";
            Chat.Print(message);
        }

        if (soundWarning)
        {
            PlaySound(soundPath, volume);
        }

        lowHpWarnings[playerName] = true;
    }

    private void CleanupLowHpWarnings()
    {   
        /* Method Description: Cleans up the lowHPWarnings dictionary so there are no memory issues after 
        impossibly long gameplay sessions.*/

        if ((DateTime.Now - lastCleanupTime).TotalMinutes > 15)
        {
            var playerName = ClientState.LocalPlayer?.Name.TextValue;

            var keysToRemove = lowHpWarnings
            .Where(kvp => kvp.Key != playerName)
            .Select(kvp => kvp.Key)
            .ToList();

            foreach (var key in keysToRemove)
            {
                lowHpWarnings.Remove(key);
            }

            lastCleanupTime = DateTime.Now;
        }
    }
    
    private async Task RunPeriodicCleanup(CancellationToken token)
    {   
        // Method description: Calls CleanupLowHpWarnings every 15 minutes
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(15), token);
            CleanupLowHpWarnings();
        }
    }

    private void PlaySound(string filePath, float volumePercent = 100f, int durationMs = 3000)
    {   
        // Method  description: Play audio file at certain volume
        if (!File.Exists(filePath))
        {
            Log.Warning($"Sound file not found: {filePath}");
            return;
        }

        var reader = new AudioFileReader(filePath)
        {
            Volume = Math.Clamp(volumePercent / 100f, 0f, 2f) // 0–200% volume
        };

        var output = new WaveOutEvent();
        output.Init(reader);
        output.Play();

        _ = Task.Run(async () =>
        {
            await Task.Delay(durationMs);
            output.Dispose();
            reader.Dispose();
        });
    }

    private void DrawUI() => WindowSystem.Draw(); // Draw loop for all ImGUI windows
    public void ToggleConfigUI() => ConfigWindow.Toggle(); // For the cog "Settings" button in plugin installer
}