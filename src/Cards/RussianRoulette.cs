
using System;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
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

    // Trefferchance in Prozent berechnen
    private decimal GetHitChance()
    {
        // Kein Besitzer, beispielsweise in der Kartenbibliothek
        if (Owner == null)
            return 75m;

        var luckPower = Owner.Creature.GetPower<LuckPower>();
        decimal luck = luckPower?.Amount ?? 0m;

        // Upgrade: Glück zählt doppelt
        decimal multiplier = IsUpgraded ? 2m : 1m;

        // 75 % Grundchance + Glück
        decimal chance = 75m + luck * multiplier;

        return Math.Clamp(chance, 0m, 100m);
    }

    // Dynamische Trefferchance in der Beschreibung
    protected override void AddExtraArgsToDescription(
        LocString description)
    {
        base.AddExtraArgsToDescription(description);

        description.Add("HitChance", GetHitChance());
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        // Aktuelle Trefferchance berechnen
        decimal hitChance = GetHitChance() / 100m;

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
        // Upgrade über IsUpgraded berücksichtigt
    }
}
