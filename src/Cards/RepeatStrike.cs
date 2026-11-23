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
    private const decimal StartChance = 70m;    // first roll, in percent
    private const decimal StepDrop = 10m;       // each further roll is this much lower
    private const decimal PointsPerLuck = 1m;   // was 2m   // percentage points per Luck
    private const int MaxHits = 20;             // safety limit

    public RepeatStrike()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move),
    ];

    private decimal GetLuck()
    {
        if (Owner == null)
            return 0m;

        return Owner.Creature.GetPower<LuckPower>()?.Amount ?? 0m;
    }

    // basePercent may be negative: the chain keeps dropping below 0
    private static decimal ChanceFor(decimal basePercent, decimal luck)
        => Math.Clamp(basePercent + PointsPerLuck * luck, 0m, 100m);

    // Chance of the first roll, shown in the card text
    private decimal GetHitChance() => ChanceFor(StartChance, GetLuck());

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);

        description.Add("HitChance", GetHitChance());
        description.Add("StepDrop", StepDrop);
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        decimal luck = GetLuck();
        int hits = 0;

        while (hits < MaxHits)
        {
            // base chance: 70, 60, 50 ... 0, -10 ...; Luck lifts every roll equally
            decimal chance = ChanceFor(StartChance - StepDrop * hits, luck) / 100m;
            float roll = RunState?.Rng.Niche.NextFloat()
                         ?? Rng.Chaotic.NextFloat();

            if (roll >= (float)chance)
                break;

            hits++;
        }

        GamblingModMain.Logger.Info(
            $"RepeatStrike: Luck={luck}, FirstChance={GetHitChance()}%, Hits={hits}"
        );

        // Failed the first roll: no damage
        if (hits == 0)
            return;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(hits)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    public override string PortraitPath =>
        "res://GamblingMod/Assets/Cards/repeat_strike.png";

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}