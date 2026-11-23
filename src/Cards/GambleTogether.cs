
using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Random;
using ModSmith.Models;
using GamblingMod.Powers;
using System.Collections.Generic;

namespace GamblingMod;

class GambleTogether : ModSmithCardModel
{
    public GambleTogether()
        : base(1, CardType.Skill,
            CardRarity.Uncommon, TargetType.Self)
    {
        //AddKeyword(CardKeyword.Exhaust);
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust
    ];

    // Erfolgschance anhand von Luck berechnen
    private static decimal GetChance(
        MegaCrit.Sts2.Core.Entities.Creatures.Creature target)
    {
        decimal luck =
            target.GetPower<LuckPower>()?.Amount ?? 0m;

        return Math.Clamp(75m + luck, 0m, 95m);
    }

    // Persönliche Chance in der Karte anzeigen
    protected override void AddExtraArgsToDescription(
        LocString description)
    {
        base.AddExtraArgsToDescription(description);

        decimal chance = 75m;

        if (Owner != null)
        {
            chance = GetChance(Owner.Creature);
        }

        description.Add("Chance", chance);
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (Owner?.Creature.CombatState == null)
            return;

        var players = Owner.Creature.CombatState.Players;

        foreach (var player in players)
        {
            var target = player.Creature;

            // Jeder Spieler hat seine eigene Chance
            decimal chance = GetChance(target);

            float roll = RunState?.Rng.Niche.NextFloat()
                         ?? Rng.Chaotic.NextFloat();

            if (roll < (float)(chance / 100m))
            {
                await PowerCmd.Apply<StrengthPower>(
                    choiceContext,
                    target,
                    3m,
                    Owner.Creature,
                    this
                );
            }
            else
            {
                await PowerCmd.Apply<FrailPower>(
                    choiceContext,
                    target,
                    3m,
                    Owner.Creature,
                    this
                );
            }

            GamblingModMain.Logger.Info(
                $"GambleTogether: Chance={chance}%, Roll={roll}"
            );
        }
    }

    public override string PortraitPath =>
        "res://GamblingMod/Assets/Cards/gamble_together.png";

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
