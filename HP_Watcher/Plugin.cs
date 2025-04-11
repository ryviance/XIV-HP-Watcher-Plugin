using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using HP_Watcher.Windows;
using FFXIVClientStructs.FFXIV.Client.UI.Arrays;
using System;

namespace HP_Watcher;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    // For messages in game from the plugin
    [PluginService] internal static IChatGui Chat { get; private set; } = null!;
    // For party list functionality
    [PluginService] internal static IPartyList PartyList { get; private set; } = null!;

    // Constants for command strings
    private const string CommandName = "/pmycommand";
    private const string HelloCommand = "/hello";
    private const string PartyHpCommand = "/php";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("HP_Watcher");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // you might normally want to embed resources and load them from the manifest stream
        var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, goatImagePath);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });

        CommandManager.AddHandler(HelloCommand, new CommandInfo(OnHelloCommand)
        {
            HelpMessage = "Prints Hello, World!"
        });

        CommandManager.AddHandler(PartyHpCommand, new CommandInfo(OnPartyHPCommand)
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

        CommandManager.RemoveHandler(CommandName);
        CommandManager.RemoveHandler(HelloCommand);
        CommandManager.RemoveHandler(PartyHpCommand);
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, just toggle the display status of our main ui
        ToggleMainUI();
    }

    private void OnHelloCommand(string command, string args)
    { 
        // Prints hello world
        Chat.Print("Hello, World!");
    }

    private void OnPartyHPCommand(string command, string args)
    {
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

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();
}
