using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Rewards;

namespace GamblingMod;

// Presentation only. Never rolls or grants a gameplay reward.
internal static class GoldGoldGoldCaseRoll
{
    private const string Assets = "res://GamblingMod/Assets/Events/";
    internal const string PreviewPath = Assets + "gold_gold_gold_case.png";
    private const double PreviewSeconds = 0.65;
    private const double SpinSeconds = 5.0;
    private const double RevealSeconds = 1.25;
    private const float TileWidth = 150f;
    private const float TileGap = 10f;
    private const float Pitch = TileWidth + TileGap;
    private const int WinnerIndex = 38;
    private const int TileCount = 46;

    private static readonly string[] Names =
        { "Gold", "Potion", "Card", "Common Relic", "Rare Relic" };
    private static readonly string[] Images =
        { "gold", "potion", "card", "relic", "rare_relic" };
    private static readonly string[] Colors =
        { "648dc8", "9a71da", "d773c1", "db785e", "eec15a" };

    private static int GetTier(Reward reward) => reward switch
    {
        GoldReward => 0,
        PotionReward => 1,
        CardReward => 2,
        RelicReward r when r.Rarity == RelicRarity.Rare => 4,
        RelicReward => 3,
        _ => throw new ArgumentException("Unknown case reward")
    };

    internal static async Task PlayAsync(Reward reward)
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
            return;

        int winner = GetTier(reward);
        CanvasLayer layer = new() { Layer = 100 };
        Tween? tween = null;
        try
        {
            Node host = tree.CurrentScene ?? tree.Root;
            host.AddChild(layer);
            Control root = new() { MouseFilter = Control.MouseFilterEnum.Stop };
            layer.AddChild(root);
            root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            // Use the logical viewport, not the physical monitor dimensions.
            Vector2 viewport = root.GetViewportRect().Size;
            AddRect(root, Vector2.Zero, viewport, new Color("101018"));
            TextureRect preview = AddImage(root, PreviewPath, Vector2.Zero, viewport);
            await root.ToSignal(tree.CreateTimer(PreviewSeconds), SceneTreeTimer.SignalName.Timeout);

            // Design at 1280x720, uniformly scaled to the logical viewport.
            float scale = Math.Min(viewport.X / 1280f, viewport.Y / 720f);
            Control stage = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
            root.AddChild(stage);
            stage.Size = new Vector2(1280, 720);
            stage.Scale = new Vector2(scale, scale);
            stage.Position = (viewport - new Vector2(1280, 720) * scale) / 2f;
            preview.Modulate = new Color(0.40f, 0.40f, 0.40f, 1f);
            AddRect(stage, new Vector2(50, 200), new Vector2(1180, 320), new Color("171820"));
            Label heading = AddLabel(stage, "OPENING CASE...", 30, new Vector2(50, 218), new Vector2(1180, 52));
            Label result = AddLabel(stage, "", 28, new Vector2(50, 452), new Vector2(1180, 52));

            const float windowWidth = 1120f;
            Control window = new() { ClipContents = true, MouseFilter = Control.MouseFilterEnum.Ignore };
            stage.AddChild(window);
            window.Position = new Vector2(80, 275);
            window.Size = new Vector2(windowWidth, 170);
            AddRect(window, Vector2.Zero, window.Size, new Color("0e1016"));
            Control strip = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
            window.AddChild(strip);
            strip.Size = new Vector2(TileCount * Pitch, 170);
            // Cosmetic sequence only; never consume the event RNG.
            int[] filler = { 0, 1, 0, 2, 0, 3, 1, 2, 0, 1, 0, 2, 4, 0, 1, 0, 2, 0, 1 };
            for (int i = 0; i < TileCount; i++)
            {
                int tier = i == WinnerIndex ? winner : filler[i % filler.Length];
                AddTile(strip, i, tier);
            }
            float center = windowWidth / 2f;
            // Exact tile center under the fixed selector at the final frame.
            float startX = center - (4 * Pitch + TileWidth / 2f);
            float endX = center - (WinnerIndex * Pitch + TileWidth / 2f);
            strip.Position = new Vector2(startX, 0);
            AddRect(window, new Vector2(center - 2, 0), new Vector2(4, 170), new Color("f9d779"));
            AddLabel(stage, "▼", 26, new Vector2(610, 250), new Vector2(60, 32));

            AudioStreamPlayer tick = new() { VolumeDb = -16f };
            stage.AddChild(tick);
            tick.Stream = GD.Load<AudioStream>(Assets + "case_tick.wav");
            int previous = 4;
            tween = root.CreateTween();
            tween.SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
            tween.TweenMethod(Callable.From<float>(x =>
            {
                strip.Position = new Vector2(x, 0);
                int current = (int)Math.Floor((center - x) / Pitch);
                if (current != previous)
                {
                    previous = current;
                    if (tick.Stream != null) tick.Play();
                }
            }), startX, endX, SpinSeconds);
            await root.ToSignal(tween, Tween.SignalName.Finished);
            strip.Position = new Vector2(endX, 0);
            heading.Text = "CASE OPENED";
            result.Text = "You found: " + Names[winner];
            result.AddThemeColorOverride("font_color", new Color(Colors[winner]));
            await root.ToSignal(tree.CreateTimer(RevealSeconds), SceneTreeTimer.SignalName.Timeout);
        }
        finally
        {
            // Remove the modal layer immediately, before native rewards appear.
            if (tween != null && tween.IsValid()) tween.Kill();
            if (GodotObject.IsInstanceValid(layer))
            {
                layer.GetParent()?.RemoveChild(layer);
                layer.QueueFree();
            }
        }
    }

    private static void AddTile(Control parent, int index, int tier)
    {
        Vector2 pos = new(index * Pitch, 5);
        Control tile = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        parent.AddChild(tile);
        tile.Position = pos;
        tile.Size = new Vector2(TileWidth, 160);
        AddRect(tile, Vector2.Zero, tile.Size, new Color(Colors[tier]));
        AddRect(tile, new Vector2(3, 3), new Vector2(TileWidth - 6, 150), new Color("26232c"));
        AddImage(tile, Assets + "case_" + Images[tier] + ".png", new Vector2(7, 8), new Vector2(136, 112));
        AddLabel(tile, Names[tier], 17, new Vector2(0, 118), new Vector2(TileWidth, 32));
    }

    private static ColorRect AddRect(Control parent, Vector2 pos, Vector2 size, Color color)
    {
        ColorRect node = new() { Color = color, MouseFilter = Control.MouseFilterEnum.Ignore };
        parent.AddChild(node);
        node.Position = pos;
        node.Size = size;
        return node;
    }

    private static TextureRect AddImage(Control parent, string path, Vector2 pos, Vector2 size)
    {
        TextureRect node = new()
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            Texture = GD.Load<Texture2D>(path)
        };
        parent.AddChild(node);
        node.Position = pos;
        node.Size = size;
        return node;
    }

    private static Label AddLabel(Control parent, string text, int fontSize, Vector2 pos, Vector2 size)
    {
        Label node = new()
        {
            Text = text,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        node.AddThemeFontSizeOverride("font_size", fontSize);
        parent.AddChild(node);
        node.Position = pos;
        node.Size = size;
        return node;
    }
}
