// File: Plugin.cs
// Description: Main plugin logic

using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Windowing;
using HP_Watcher.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
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
    private const string PartyHpCommand = "/php";

    // Variables for method utilities
    private readonly Dictionary<string, bool> lowHpWarnings = new(); // Used in CheckHP to not spam warnings
    private DateTime lastCleanupTime = DateTime.Now; // Keep track of time to purge readonly dictionary of unused keys

    public Configuration Configuration { get; init; } // Initialize config data

    // ImGUI windows
    public readonly WindowSystem WindowSystem = new("HP_Watcher");
    private ConfigWindow ConfigWindow { get; init; }

    private CancellationTokenSource? cleanupTaskToken; // Threading for cleanup

    public Plugin()
    {   
        // Initialize and load previous config data from Configuration.cs
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration(); 
        
        // Initialize and draw ImGUI config window
        ConfigWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(ConfigWindow);

        // UIBuilder.Draw is a hook for every-frame logic, not exclusively used for UI:
        PluginInterface.UiBuilder.Draw += CheckHp; // Check HP of party members and player every frame
        PluginInterface.UiBuilder.Draw += DrawUI; // Check every frame if it needs to draw config window (due to button/command, etc.)
        
        // Clean HP Warning dictionary using threading
        cleanupTaskToken = new CancellationTokenSource();
        _ = RunPeriodicCleanup(cleanupTaskToken.Token); // Wait every 15 minutes to clean

        // Add commands and their help messages
        CommandManager.AddHandler(PartyHpCommand, new CommandInfo(OnPartyHpCommand)
        {
            HelpMessage = "Displays HP of self and party members."
        });

        // Add functionality to "Open" button in the plugin installer
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
                if (member.Name == player?.Name && member.World.RowId == player?.HomeWorld.RowId) // Skip self
                {
                    continue;
                }
                Chat.Print($"{member.Name} - {member.CurrentHP}/{member.MaxHP} HP");
            }
        }
    }

    private void CheckHp()
    {   
        // Method description: Outputs a warning message if party member or player falls below threshold
        float threshold = Configuration.HpThresholdPercent / 100f;
        bool chatWarningEnabled = Configuration.ChatWarningEnabled;

        var player = ClientState.LocalPlayer;
        if (player != null)
        {
            string playerKey = player.Name.TextValue;
            float hpPercent = (float)player.CurrentHp / player.MaxHp;

            if (hpPercent >= threshold)
            {
                lowHpWarnings[playerKey] = false;
            }
            else if (!lowHpWarnings.GetValueOrDefault(playerKey, false))
            {
                if (chatWarningEnabled)
                {
                    Chat.Print($"You are below {Configuration.HpThresholdPercent}% HP! ({player.CurrentHp}/{player.MaxHp})");
                }
                lowHpWarnings[playerKey] = true;
            }
        }

        foreach (var member in PartyList)
        {
            string memberKey = member.Name.TextValue;
            float partyHpPercent = (float)member.CurrentHP / member.MaxHP;

            if (partyHpPercent >= threshold)
            {
                lowHpWarnings[memberKey] = false;
                continue;
            }

            if (!lowHpWarnings.GetValueOrDefault(memberKey, false))
            {
                if (chatWarningEnabled)
                {
                    Chat.Print($"{member.Name} is below {Configuration.HpThresholdPercent}% HP! ({member.CurrentHP}/{member.MaxHP})");
                }
                lowHpWarnings[memberKey] = true;
            }
        }
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

    private void DrawUI() => WindowSystem.Draw(); // Draw loop for all ImGUI windows
    public void ToggleConfigUI() => ConfigWindow.Toggle(); // For the cog "Settings" button in plugin installer
}