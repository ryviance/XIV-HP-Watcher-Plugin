using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace HP_Watcher.Windows;

public class OverlayWindow : Window, IDisposable
{
    private Configuration config;
    private Vector2 overlaySize = new Vector2(200, 20); // default size
    private Vector2 overlayPos = new Vector2(100, 100); // default position

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

        if (!config.OverlayUnlocked)
        {
            Flags |= ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;
        }

        Size = overlaySize;
        SizeCondition = ImGuiCond.FirstUseEver;

        Position = overlayPos;
        PositionCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        var drawList = ImGui.GetWindowDrawList();
        var pos = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();

        // Save position/size while unlocked
        if (config.OverlayUnlocked)
        {   
            ImGui.PushStyleColor(ImGuiCol.ResizeGrip, new Vector4(0, 0, 0, 0));
            ImGui.SetNextWindowSizeConstraints(Vector2.Zero, new Vector2(float.MaxValue, float.MaxValue));
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
}