
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;
using ModSmith.Models;

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
        // Zufallszahl zwischen 0 und 1
        float roll = RunState?.Rng.Niche.NextFloat()
                     ?? Rng.Chaotic.NextFloat();

        if (roll < 0.75f)
        {
            // 75 %: Ausgewählten Gegner töten
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
            // 25 %: Eigene HP auf 1 setzen
            NCombatRoom.Instance?.PlaySplashVfx(
                Owner.Creature, StsColors.red);

            await CreatureCmd.SetCurrentHp(
                Owner.Creature, 1);
        }
    }

    //Bild Path angeben (Chat sagt Bilder sollten 250 × 190 oder 1000 × 760 Pixel groß sein)
    public override string PortraitPath => "res://GamblingMod/Assets/Cards/russian_roulette.png"; 


    protected override void OnUpgrade()
    {
    // Kein zusätzlicher Upgrade-Effekt
    }
}
