using System;
using System.Threading.Tasks;
using Godot;

namespace GamblingMod;

public static class GoldGoldGoldAnimation
{
    // ==========================================
    // ANIMATION SETTINGS
    // ==========================================

    private const float RollDuration = 4.0f;
    private const float ResultDuration = 1.5f;
    private const float JackpotDuration = 2.5f;

    private const int TileCount = 65;
    private const int WinnerIndex = 55;

    private const float TileWidth = 150f;
    private const float TileHeight = 170f;
    private const float TileGap = 8f;

    private const float WindowWidth = 950f;
    private const float WindowHeight = 210f;

    private static readonly Color[] RarityColors =
    {
        new Color("#4B79D8"),
        new Color("#9655DB"),
        new Color("#D64BB7"),
        new Color("#E34B4B"),
        new Color("#DCAE32")
    };

    private static readonly string[] RarityNames =
    {
        "BLUE",
        "PURPLE",
        "PINK",
        "RED",
        "GOLD"
    };

    private static readonly string[] Rewards =
    {
        "5 GOLD",
        "50 GOLD",
        "150 GOLD",
        "500 GOLD",
        "RARE RELIC"
    };

    // ==========================================
    // MAIN ANIMATION
    // ==========================================

    public static async Task Play(
        Node parent,
        int winner
    )
    {
        if (!parent.IsInsideTree())
            return;

        SceneTree tree = parent.GetTree();

        // Eigenstaendige Darstellung ueber dem Event.

        CanvasLayer layer = new()
        {
            Layer = 100
        };

        parent.AddChild(layer);

        try
        {
            // Hintergrund abdunkeln.

            ColorRect background = new()
            {
                Color = new Color(0.02f, 0.02f, 0.04f, 0.95f),
                MouseFilter = Control.MouseFilterEnum.Stop
            };

            layer.AddChild(background);

            background.SetAnchorsAndOffsetsPreset(
                Control.LayoutPreset.FullRect
            );

            // Zentrierter Bereich.

            CenterContainer center = new()
            {
                MouseFilter = Control.MouseFilterEnum.Ignore
            };

            background.AddChild(center);

            center.SetAnchorsAndOffsetsPreset(
                Control.LayoutPreset.FullRect
            );

            VBoxContainer layout = new()
            {
                MouseFilter = Control.MouseFilterEnum.Ignore
            };

            layout.AddThemeConstantOverride(
                "separation",
                22
            );

            center.AddChild(layout);

            Label title = MakeLabel(
                "GOLD GOLD GOLD",
                44,
                RarityColors[4]
            );

            layout.AddChild(title);

            Label status = MakeLabel(
                "Opening case...",
                25,
                Colors.White
            );

            layout.AddChild(status);

            // ======================================
            // Sichtfenster
            // ======================================

            Control window = new()
            {
                CustomMinimumSize = new Vector2(
                    WindowWidth,
                    WindowHeight
                ),

                ClipContents = true,

                MouseFilter =
                    Control.MouseFilterEnum.Ignore
            };

            layout.AddChild(window);

            ColorRect windowBackground = new()
            {
                Color = new Color("#17171F"),

                Size = new Vector2(
                    WindowWidth,
                    WindowHeight
                ),

                MouseFilter =
                    Control.MouseFilterEnum.Ignore
            };

            window.AddChild(windowBackground);

            // Beweglicher Streifen mit Belohnungen.

            Control strip = new()
            {
                Position = new Vector2(0f, 20f),

                MouseFilter =
                    Control.MouseFilterEnum.Ignore
            };

            window.AddChild(strip);

            Panel winnerTile = null!;

            // Separater kosmetischer Zufallsgenerator.
            // Hat keinen Einfluss auf den Spiel-RNG.

            System.Random visualRng = new();

            for (int i = 0; i < TileCount; i++)
            {
                int rarity = i == WinnerIndex
                    ? winner
                    : GetVisualRarity(visualRng);

                Panel tile = CreateTile(rarity);

                tile.Position = new Vector2(
                    i * (TileWidth + TileGap),
                    0f
                );

                strip.AddChild(tile);

                if (i == WinnerIndex)
                    winnerTile = tile;
            }

            // ======================================
            // Zentraler Auswahlzeiger
            // ======================================

            ColorRect marker = new()
            {
                Color = new Color("#FFE4A1"),

                Position = new Vector2(
                    WindowWidth / 2f - 2f,
                    0f
                ),

                Size = new Vector2(
                    4f,
                    WindowHeight
                ),

                MouseFilter =
                    Control.MouseFilterEnum.Ignore
            };

            window.AddChild(marker);

            Label result = MakeLabel(
                "",
                35,
                Colors.White
            );

            result.CustomMinimumSize =
                new Vector2(0f, 60f);

            layout.AddChild(result);

            // ======================================
            // Zielposition berechnen
            // ======================================

            float winnerCenter =
                WinnerIndex * (TileWidth + TileGap)
                + TileWidth / 2f;

            float finalX =
                WindowWidth / 2f - winnerCenter;

            // Schnell starten und langsam abbremsen.

            Tween rollTween = strip.CreateTween();

            rollTween.SetTrans(
                Tween.TransitionType.Quint
            );

            rollTween.SetEase(
                Tween.EaseType.Out
            );

            rollTween.TweenProperty(
                strip,
                "position",
                new Vector2(finalX, 20f),
                RollDuration
            );

            await parent.ToSignal(
                tree.CreateTimer(RollDuration + 0.05f),
                SceneTreeTimer.SignalName.Timeout
            );

            if (!GodotObject.IsInstanceValid(layer))
                return;

            // Exakt mittig auf dem Gewinn anhalten.

            strip.Position =
                new Vector2(finalX, 20f);

            // ======================================
            // Gewinn hervorheben
            // ======================================

            Color winningColor =
                RarityColors[winner];

            status.Text = RarityNames[winner];

            status.AddThemeColorOverride(
                "font_color",
                winningColor
            );

            result.Text = Rewards[winner];

            result.AddThemeColorOverride(
                "font_color",
                winningColor
            );

            winnerTile.PivotOffset = new Vector2(
                TileWidth / 2f,
                TileHeight / 2f
            );

            winnerTile.ZIndex = 2;
            marker.ZIndex = 3;

            Tween highlight =
                winnerTile.CreateTween();

            highlight.SetTrans(
                Tween.TransitionType.Sine
            );

            highlight.SetEase(
                Tween.EaseType.InOut
            );

            highlight.TweenProperty(
                winnerTile,
                "scale",
                new Vector2(1.12f, 1.12f),
                0.25f
            );

            highlight.TweenProperty(
                winnerTile,
                "scale",
                Vector2.One,
                0.25f
            );

            // ======================================
            // GOLD JACKPOT
            // ======================================

            if (winner == 4)
            {
                title.Text = "GOLD! GOLD! GOLD!";

                title.AddThemeColorOverride(
                    "font_color",
                    new Color("#FFE47A")
                );

                status.Text = "JACKPOT";

                CreateJackpotParticles(background);

                Tween flash = background.CreateTween();

                flash.TweenProperty(
                    background,
                    "color",
                    new Color(
                        0.55f,
                        0.38f,
                        0.06f,
                        0.98f
                    ),
                    0.20f
                );

                flash.TweenProperty(
                    background,
                    "color",
                    new Color(
                        0.02f,
                        0.02f,
                        0.04f,
                        0.95f
                    ),
                    0.65f
                );
            }

            float duration = winner == 4
                ? JackpotDuration
                : ResultDuration;

            await parent.ToSignal(
                tree.CreateTimer(duration),
                SceneTreeTimer.SignalName.Timeout
            );
        }
        finally
        {
            // Overlay nach der Animation entfernen.

            if (GodotObject.IsInstanceValid(layer))
            {
                layer.QueueFree();
            }
        }
    }

    // ==========================================
    // EINZELNES BELOHNUNGSFELD
    // ==========================================

    private static Panel CreateTile(int rarity)
    {
        Color color = RarityColors[rarity];

        Panel tile = new()
        {
            Size = new Vector2(
                TileWidth,
                TileHeight
            ),

            MouseFilter =
                Control.MouseFilterEnum.Ignore
        };

        StyleBoxFlat style = new()
        {
            BgColor = color.Darkened(0.76f),

            BorderColor = color,

            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            BorderWidthBottom = 5,

            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6
        };

        tile.AddThemeStyleboxOverride(
            "panel",
            style
        );

        VBoxContainer content = new()
        {
            Alignment =
                BoxContainer.AlignmentMode.Center,

            MouseFilter =
                Control.MouseFilterEnum.Ignore
        };

        content.AddThemeConstantOverride(
            "separation",
            8
        );

        tile.AddChild(content);

        content.SetAnchorsAndOffsetsPreset(
            Control.LayoutPreset.FullRect
        );

        // Symbol fuer Gold oder Relikt.

        Label symbol = MakeLabel(
            rarity == 4 ? "◆" : "●",
            56,
            RarityColors[4]
        );

        content.AddChild(symbol);

        Label rarityLabel = MakeLabel(
            RarityNames[rarity],
            19,
            color
        );

        content.AddChild(rarityLabel);

        Label rewardLabel = MakeLabel(
            Rewards[rarity],
            18,
            Colors.White
        );

        content.AddChild(rewardLabel);

        return tile;
    }

    // ==========================================
    // LABEL HELPER
    // ==========================================

    private static Label MakeLabel(
        string text,
        int fontSize,
        Color color
    )
    {
        Label label = new()
        {
            Text = text,

            HorizontalAlignment =
                HorizontalAlignment.Center,

            VerticalAlignment =
                VerticalAlignment.Center,

            MouseFilter =
                Control.MouseFilterEnum.Ignore
        };

        label.AddThemeFontSizeOverride(
            "font_size",
            fontSize
        );

        label.AddThemeColorOverride(
            "font_color",
            color
        );

        return label;
    }

    // ==========================================
    // KOSMETISCHE BELOHNUNGEN
    // ==========================================

    private static int GetVisualRarity(
        System.Random rng
    )
    {
        int roll = rng.Next(10000);

        return roll switch
        {
            < 7992 => 0,
            < 9590 => 1,
            < 9910 => 2,
            < 9974 => 3,
            _      => 4
        };
    }

    // ==========================================
    // JACKPOT PARTIKEL
    // ==========================================

    private static void CreateJackpotParticles(
        Control parent
    )
    {
        System.Random rng = new();

        Vector2 viewportSize =
            parent.GetViewportRect().Size;

        Vector2 origin =
            viewportSize / 2f;

        for (int i = 0; i < 65; i++)
        {
            ColorRect particle = new()
            {
                Color = i % 2 == 0
                    ? new Color("#FFD700")
                    : new Color("#FFF0A0"),

                Size = new Vector2(8f, 8f),

                Position = origin,

                ZIndex = 10,

                MouseFilter =
                    Control.MouseFilterEnum.Ignore
            };

            parent.AddChild(particle);

            double angle =
                rng.NextDouble() * Math.PI * 2.0;

            float distance =
                180f + (float)rng.NextDouble() * 650f;

            Vector2 direction = new(
                (float)Math.Cos(angle),
                (float)Math.Sin(angle)
            );

            Vector2 target =
                origin + direction * distance;

            float duration =
                0.8f + (float)rng.NextDouble();

            Tween tween = particle.CreateTween();

            tween.SetParallel(true);

            tween.TweenProperty(
                particle,
                "position",
                target,
                duration
            ).SetTrans(
                Tween.TransitionType.Cubic
            ).SetEase(
                Tween.EaseType.Out
            );

            tween.TweenProperty(
                particle,
                "modulate",
                new Color(1f, 1f, 1f, 0f),
                duration
            );
        }
    }
}