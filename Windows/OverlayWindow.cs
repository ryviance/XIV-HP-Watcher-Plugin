using System;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace HP_Watcher.Windows;

public class OverlayWindow : Window, IDisposable
{
    private Configuration config;
    private bool pushedStyles = false; // Safety variable to prevent style leaks
    private Vector2 relativePos, overlaySize, overlayPos;

    public OverlayWindow(Configuration config)
        : base("###HP_Watcher_Overlay")
    {
        this.config = config;
        IsOpen = true;
        RespectCloseHotkey = false;
        relativePos = config.OverlayRelativePosition;
        overlaySize = config.OverlaySize;

    }

    public void Dispose() { }

    public override void PreDraw()
    {
        Flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings |
                ImGuiWindowFlags.NoBackground;

        // Always push style (whether locked or unlocked)
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(5f, 5f));
        ImGui.PushStyleColor(ImGuiCol.ResizeGrip, new Vector4(0, 0, 0, 0)); // Hide grip
        ImGui.SetNextWindowSizeConstraints(Vector2.Zero, new Vector2(float.MaxValue, float.MaxValue));
        pushedStyles = true;

        if (config.OverlayLocked)
        {
            Flags |= ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;
        }

        Size = overlaySize;
        SizeCondition = ImGuiCond.FirstUseEver;

        var windowSize = ImGui.GetMainViewport().Size;

        overlayPos = new Vector2(
            windowSize.X * relativePos.X,
            windowSize.Y * overlaySize.Y
        );

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
            var windowSize = ImGui.GetMainViewport().Size;

            // Save position/size while unlocked
            if (!config.OverlayLocked)
            {   
                var newRelativePos = new Vector2(
                    pos.X / windowSize.X,
                    pos.Y / windowSize.Y
                );

                bool dirty = false;

                if (newRelativePos != relativePos)
                {   
                    relativePos = newRelativePos;
                    config.OverlayRelativePosition = newRelativePos;
                    dirty = true;
                }

                if (size != overlaySize)
                {   
                    overlaySize = size;
                    config.OverlaySize = size;
                    dirty = true;
                }

                if (dirty)
                {
                    config.Save();
                }
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