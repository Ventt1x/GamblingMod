
using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;
using ModSmith.Models;
using GamblingMod.Powers;

namespace GamblingMod;

class RepeatStrike : ModSmithCardModel
{
    public RepeatStrike()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move),
    ];

    // Trefferchance in Prozent berechnen
    private decimal GetHitChance()
    {
        if (Owner == null)
            return 50m;

        var luckPower = Owner.Creature.GetPower<LuckPower>();
        decimal luck = luckPower?.Amount ?? 0m;

        // 50 % Grundchance + 1 Prozentpunkt pro Glück
        decimal chance = 50m + luck;

        // Maximale Trefferchance: 95 %
        return Math.Clamp(chance, 0m, 95m);
    }

    // Dynamische Trefferchance in der Kartenbeschreibung
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
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        // Aktuelle Trefferchance
        decimal hitChance = GetHitChance() / 100m;

        // Jeder erfolgreiche Wurf erzeugt einen Treffer
        int hits = 0;

        while (hits < 200)
        {
            float roll = RunState?.Rng.Niche.NextFloat()
                         ?? Rng.Chaotic.NextFloat();

            if (roll >= (float)hitChance)
                break;

            hits++;
        }

        GamblingModMain.Logger.Info(
            $"RepeatStrike: Chance={hitChance:P0}, Hits={hits}"
        );

        // Kein Treffer beim ersten Fehlwurf
        if (hits == 0)
            return;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(hits)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
