
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

    // Wahrscheinlichkeit für Kopf berechnen (in Prozent)
    private decimal GetHeadsChance()
    {
        if (Owner == null)
            return 50m;

        var luckPower = Owner.Creature.GetPower<LuckPower>();
        decimal luck = luckPower?.Amount ?? 0m;

        // 50 % Grundchance + 1 Prozentpunkt pro Glück
        decimal chance = 50m + luck;

        return Math.Clamp(chance, 0m, 100m);
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

    protected override void OnUpgrade()
    {
        // Gewinn von 5 auf 7 erhöhen
        DynamicVars["LuckPower"].UpgradeValueBy(2m);
    }
}
