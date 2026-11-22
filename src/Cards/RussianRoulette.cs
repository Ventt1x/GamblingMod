
using System;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using ModSmith.Models;
using GamblingMod.Powers;

namespace GamblingMod;

class RussianRoulette : ModSmithCardModel
{
    public RussianRoulette()
        : base(2, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        // Aktuelle Glückspunkte des Spielers auslesen
        var luckPower = Owner.Creature.GetPower<LuckPower>();
        decimal luck = luckPower?.Amount ?? 0m;

        // Beim Upgrade zählt jeder Glückspunkt doppelt
        decimal luckMultiplier = IsUpgraded ? 2m : 1m;

        // Grundchance 75 %, +1 Prozentpunkt pro Glück
        decimal hitChance = 0.75m + luck * luckMultiplier * 0.01m;

        // Wahrscheinlichkeit auf 0 bis 100 % begrenzen
        hitChance = Math.Clamp(hitChance, 0m, 1m);

        // Zufallszahl zwischen 0 und 1
        float roll = RunState?.Rng.Niche.NextFloat()
                     ?? Rng.Chaotic.NextFloat();

        if (roll < (float)hitChance)
        {
            // Erfolg: Ausgewählten Gegner töten
            var target = cardPlay.Target;

            if (target != null)
            {
                NCombatRoom.Instance?.PlaySplashVfx(
                    target, StsColors.green);

                await CreatureCmd.Kill(target);
            }
        }
        else
        {
            // Misserfolg: Eigene HP auf 1 setzen
            NCombatRoom.Instance?.PlaySplashVfx(
                Owner.Creature, StsColors.red);

            await CreatureCmd.SetCurrentHp(
                Owner.Creature, 1);
        }
    }

    public override string PortraitPath =>
        "res://GamblingMod/Assets/Cards/russian_roulette.png";

    protected override void OnUpgrade()
    {
        // Upgrade wird über IsUpgraded berücksichtigt
    }
}
