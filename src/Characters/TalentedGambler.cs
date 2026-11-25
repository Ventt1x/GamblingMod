using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Relics;

namespace GamblingMod;

public sealed class TalentedGambler : CharacterModel
{
    // Temporarily use the Ironclad's gameplay defaults and visual style.
    private static Ironclad Original => ModelDb.Character<Ironclad>();

    public override CharacterGender Gender => CharacterGender.Masculine;
    protected override CharacterModel? UnlocksAfterRunAs => null;
    public override int StartingHp => 67;
    public override int StartingGold => 69;
    public override int MaxEnergy => 3;
    public override Color NameColor => Original.NameColor;

    public override CardPoolModel CardPool => ModelDb.CardPool<TalentedGamblerCardPool>();
    public override PotionPoolModel PotionPool => Original.PotionPool;
    public override RelicPoolModel RelicPool => Original.RelicPool;
    public override float AttackAnimDelay => Original.AttackAnimDelay;
    public override float CastAnimDelay => Original.CastAnimDelay;
    public override Color EnergyLabelOutlineColor => Original.EnergyLabelOutlineColor;
    public override Color DialogueColor => Original.DialogueColor;
    public override VfxColor SpeechBubbleColor => Original.SpeechBubbleColor;
    public override Color MapDrawingColor => Original.MapDrawingColor;
    public override Color RemoteTargetingLineColor => Original.RemoteTargetingLineColor;
    public override Color RemoteTargetingLineOutline => Original.RemoteTargetingLineOutline;
    public override float DreadBgBrightness => Original.DreadBgBrightness;
    public override float DreadBgContrast => Original.DreadBgContrast;
    public override Color DreadNameColor => Original.DreadNameColor;

    public override string CharacterSelectSfx => Original.CharacterSelectSfx;
    public override string DeathSfx => Original.DeathSfx;
    public override string CharacterTransitionSfx => Original.CharacterTransitionSfx;

    protected override string IconPath => GamblerAssets.Resolve("scenes/ui/character_icons/ironclad_icon.tscn");
    protected override string CharacterSelectIconPath => GamblerAssets.Resolve("images/packed/character_select/char_select_ironclad.png");
    protected override string CharacterSelectLockedIconPath => GamblerAssets.Resolve("images/packed/character_select/char_select_ironclad_locked.png");
    protected override string MapMarkerPath => GamblerAssets.Resolve("images/packed/map/icons/map_marker_ironclad.png");


    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<RepeatStrike>(),
        ModelDb.Card<RussianRoulette>(),
        ModelDb.Card<RepeatStrike>(),
        ModelDb.Card<LuckyDay>(),
        ModelDb.Card<GambleTogether>(),
        ModelDb.Card<FortuneShield>(),
        ModelDb.Card<CoinFlip>(),
        ModelDb.Card<FortuneShield>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<BowlerHat>()
    ];

    protected override List<(AnimState, string)> AnimationStates
    {
        get
        {
            var states = base.AnimationStates;
            states.Add((new AnimState("attack_heavy"), "heavyAttack"));
            return states;
        }
    }
    public override List<string> GetArchitectAttackVfx() => Original.GetArchitectAttackVfx();
}
