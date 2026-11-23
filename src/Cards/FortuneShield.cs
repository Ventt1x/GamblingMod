using System;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;
using ModSmith.Models;
using GamblingMod.Powers;

namespace GamblingMod;

class FortuneShield : ModSmithCardModel
{
    private const decimal PointsPerLuck = 1m;   // keep equal to RepeatStrike
    private const int UpgradeShift = 2;         // upgraded: floor and max are raised by this

    public FortuneShield()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    private decimal GetLuck()
    {
        if (Owner == null)
            return 0m;

        return Owner.Creature.GetPower<LuckPower>()?.Amount ?? 0m;
    }

    private int Shift => IsUpgraded ? UpgradeShift : 0;

    // Range shown in the card text: 1-10, or 3-12 when upgraded
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);

        description.Add("Min", 1 + Shift);
        description.Add("Max", 10 + Shift);
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        float r = RunState?.Rng.Niche.NextFloat() ?? Rng.Chaotic.NextFloat();
        int roll = 1 + (int)(r * 100f);                  // 1..100

        decimal total = roll + PointsPerLuck * GetLuck();
        int block = (int)Math.Clamp(Math.Ceiling(total / 10m), 1m, 10m) + Shift;

        GamblingModMain.Logger.Info(
            $"FortuneShield: roll={roll}, luck={GetLuck()}, total={total}, block={block}"
        );

        if (block <= 0)
            return;

        await CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Move, cardPlay);
    }

    public override string PortraitPath =>
        "res://GamblingMod/Assets/Cards/fortune_shield.png";


    protected override void OnUpgrade()
    {
        // The shift is applied in OnPlay and in the description via IsUpgraded
    }
}