
using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;
using ModSmith.Models;
using GamblingMod.Powers;
using GamblingMod.Utils;

using MegaCrit.Sts2.Core.HoverTips;

namespace GamblingMod;

class CoinFlip : ModSmithCardModel
{
    public CoinFlip()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<LuckPower>(5m)
    ];

    // Zusätzlicher Hilfstext beim Hovern über die Karte
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        new HoverTip(
            new LocString("cards", $"{Id.Entry}.luckTooltipTitle"),
            new LocString("cards", $"{Id.Entry}.luckTooltipDescription")
        )
    ];

    // Wahrscheinlichkeit für Kopf berechnen (in Prozent)
    private decimal GetHeadsChance()
    {
        if (Owner == null)
            return 50m;

        decimal chance = LuckCalculator.Calculate(
            creature: Owner.Creature,
            startChance: 50m,
            minChance: 5m,
            maxChance: 90m,
            curveFactor: 1.00d,
            linearSlopeFactor: 1.00d
        );

        return Math.Round(chance, 1, MidpointRounding.AwayFromZero);
    }
    // Aktuelle Kopf-Wahrscheinlichkeit in Kartenbeschreibung
    protected override void AddExtraArgsToDescription(
        LocString description)
    {
        base.AddExtraArgsToDescription(description);

        description.Add("HeadsChance", GetHeadsChance());
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        // Aktuelle Wahrscheinlichkeit berechnen
        decimal headsChance = GetHeadsChance() / 100m;

        // Zufallszahl zwischen 0 und 1
        float roll = RunState?.Rng.Niche.NextFloat()
                     ?? Rng.Chaotic.NextFloat();

        bool heads = roll < (float)headsChance;

        decimal luckValue = DynamicVars["LuckPower"].BaseValue;

        if (heads)
        {
            // Kopf: Glück erhalten
            TalkCmd.Play(
                new LocString("cards", $"{base.Id.Entry}.headsBanter"),
                Owner.Creature,
                VfxColor.Green,
                VfxDuration.Standard
            );

            NCombatRoom.Instance?.PlaySplashVfx(
                Owner.Creature,
                StsColors.green
            );

            await PowerCmd.Apply<LuckPower>(
                choiceContext,
                Owner.Creature,
                luckValue,
                Owner.Creature,
                this
            );
        }
        else
        {
            // Zahl: Glück verlieren
            TalkCmd.Play(
                new LocString("cards", $"{base.Id.Entry}.tailsBanter"),
                Owner.Creature,
                VfxColor.Red,
                VfxDuration.Standard
            );

            NCombatRoom.Instance?.PlaySplashVfx(
                Owner.Creature,
                StsColors.red
            );

            // Beim Upgrade: Verlust halbieren und aufrunden
            decimal loss = IsUpgraded
                ? Math.Ceiling(luckValue / 2m)
                : luckValue;

            // Negatives Glück ist erlaubt
            await PowerCmd.Apply<LuckPower>(
                choiceContext,
                Owner.Creature,
                -loss,
                Owner.Creature,
                this
            );
        }
    }

    public override string PortraitPath =>
        "res://GamblingMod/Assets/Cards/coin_flip.png";

    protected override void OnUpgrade()
    {
        // Gewinn von 5 auf 7 erhöhen
        DynamicVars["LuckPower"].UpgradeValueBy(2m);
    }
}
