
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using ModSmith.Models;
using GamblingMod.Powers;

namespace GamblingMod;

public sealed class LuckyDay : ModSmithCardModel
{
    public LuckyDay()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<LuckPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<LuckPower>(3m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(
            Owner.Creature,
            "PowerUp",
            Owner.Character.PowerUpAnimDelay
        );

        await PowerCmd.Apply<LuckPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["LuckPower"].BaseValue,
            Owner.Creature,
            this
        );
    }

    public override string PortraitPath =>
        "res://GamblingMod/Assets/Cards/lucky_day.png";

    protected override void OnUpgrade()
    {
        DynamicVars["LuckPower"].UpgradeValueBy(1m);
    }
}
