using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.Internal;
using Dalamud.Plugin.Services;
using HP_Watcher.Windows;
using FFXIVClientStructs.FFXIV.Client.UI.Arrays;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HP_Watcher;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IChatGui Chat { get; private set; } = null!;  // For system messages in game from the plugin
    [PluginService] internal static IPartyList PartyList { get; private set; } = null!; // For party list functionality

    // Constants for command strings
    private const string PartyHpCommand = "/php";

    // Constants for method utilities
    private readonly Dictionary<string, bool> lowHpWarnings = new(); // Used in CheckHP to not spam warnings
    private DateTime lastCleanupTime = DateTime.Now; // Keep track of time to purge readonly dictionary of unused keys

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("HP_Watcher");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    private CancellationTokenSource? cleanupTaskToken;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // you might normally want to embed resources and load them from the manifest stream
        var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, goatImagePath);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        // Polling (run every frame)
        PluginInterface.UiBuilder.Draw += CheckHp; // Check HP 
        
        // Clean HP Warning dictionary
        cleanupTaskToken = new CancellationTokenSource();
        _ = RunPeriodicCleanup(cleanupTaskToken.Token); // Wait every 15 minutes to clean

        CommandManager.AddHandler(PartyHpCommand, new CommandInfo(OnPartyHpCommand)
        {
            HelpMessage = "Displays HP of self and party members."
        });

        // This adds a button to the plugin installer entry of this plugin which allows
        // to toggle the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

        // Adds another button that is doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

        // Add a simple message to the log with level set to information
        // Use /xllog to open the log window in-game
        // Example Output: 00:57:54.959 | INF | [HP_Watcher] ===A cool log message from HP_Watcher===
        Log.Information($"===A cool log message from {PluginInterface.Manifest.Name}===");
    }

    public void Dispose()
    {   
        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();
        MainWindow.Dispose();
        cleanupTaskToken?.Cancel(); // Stop cleaning dictionary on shutdown

        CommandManager.RemoveHandler(PartyHpCommand);
        
        
        PluginInterface.UiBuilder.Draw -= CheckHp; // Stop checking HP every frame
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, just toggle the display status of our main ui
        ToggleMainUI();
    }

    private void OnPartyHpCommand(string command, string args)
    {   
        // Method description: Displays HP of player and all party members in chat.
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
        var player = ClientState.LocalPlayer;
        if (player != null)
        {   
            string playerKey = player.Name.TextValue;
            float hpPercent = (float)player.CurrentHp / player.MaxHp;

            if (hpPercent < 0.6f)
            {   
                if (!lowHpWarnings.GetValueOrDefault(playerKey, false))
                {
                    Chat.Print($"WARNING: You are below 60% HP! ({player.CurrentHp}/{player.MaxHp})");
                    lowHpWarnings[playerKey] = true;
                }
            } else {
                lowHpWarnings[playerKey] = false;
            }
        }

        foreach (var member in PartyList)
        {   
            string memberKey = member.Name.TextValue;
            float partyHpPercent = (float)member.CurrentHP / member.MaxHP;

            if (partyHpPercent < 0.6f)
            {   
                if (!lowHpWarnings.GetValueOrDefault(memberKey, false))
                {
                    Chat.Print($"WARNING: {member.Name} is below 60% HP! ({member.CurrentHP}/{member.MaxHP})");
                    lowHpWarnings[memberKey] = true;
                }
            } else {
                lowHpWarnings[memberKey] = false;
            }
        }
    }

    private void DrawUI() => WindowSystem.Draw();

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


    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();
}