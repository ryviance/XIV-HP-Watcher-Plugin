using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace HP_Watcher.Windows;

public class OverlayWindow : Window, IDisposable
{
    private Configuration config;
    private Vector2 overlaySize = new Vector2(200, 20); // Default size
    private Vector2 overlayPos = new Vector2(100, 100); // Default position
    private bool pushedStyles = false; // Safety variable to prevent style leaks

    public OverlayWindow(Configuration config)
        : base("###HP_Watcher_Overlay")
    {
        this.config = config;
        IsOpen = true;
        RespectCloseHotkey = false;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        Flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings |
                ImGuiWindowFlags.NoBackground;

        if (config.OverlayLocked)
        {
            Flags |= ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;
        }
        else
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(5f, 5f));
            ImGui.PushStyleColor(ImGuiCol.ResizeGrip, new Vector4(0, 0, 0, 0)); // Hide grip
            ImGui.SetNextWindowSizeConstraints(Vector2.Zero, new Vector2(float.MaxValue, float.MaxValue));
            pushedStyles = true;
        }

        Size = overlaySize;
        SizeCondition = ImGuiCond.FirstUseEver;

        Position = overlayPos;
        PositionCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        // Ensure the plugin is completely safe from crashing or causing global GUI issues.
        // The ImGui style stack is GLOBAL across all plugins and windows, so we must
        // guarantee that everything pushed onto the stack is properly popped.
        try
        {
            var drawList = ImGui.GetWindowDrawList();
            var pos = ImGui.GetWindowPos();
            var size = ImGui.GetWindowSize();

            // Save position/size while unlocked
            if (!config.OverlayLocked)
            {
                overlayPos = pos;
                overlaySize = size;
            }

            // Draw translucent red rectangle
            drawList.AddRectFilled(
                pos,
                new Vector2(pos.X + size.X, pos.Y + size.Y),
                ImGui.GetColorU32(new Vector4(0.9f, 0.1f, 0.1f, 0.4f))
            );
        }
        finally
        {
            if (pushedStyles)
            {
                ImGui.PopStyleColor();
                ImGui.PopStyleVar(2);
                pushedStyles = false;
            }
        }
    }
}