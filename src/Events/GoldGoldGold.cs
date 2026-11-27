using System.Collections.Generic;
using System.Threading.Tasks;

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

using ModSmith.Models;

namespace GamblingMod;

public sealed class GoldGoldGold : ModSmithEventModel
{
    // Settings
    private const int CasePrice = 21;

    private const int GoldChance = 50;
    private const int PotionChance = 25;
    private const int CardChance = 20;
    private const int CommonRelicChance = 4;
    private const int RareRelicChance = 1;

    private const int MinGoldReward = 10;
    private const int MaxGoldReward = 20;

    public override bool IsShared => false;

    public override EventLayoutType LayoutType =>
        EventLayoutType.Default;

    protected override string InitialPortraitPath =>
        "res://GamblingMod/Assets/Events/gold_gold_gold_case.png";

    protected override IReadOnlyList<EventOption>
        GenerateInitialOptions()
    {
        EventOption openOption;

        if (Owner != null && Owner.Gold >= CasePrice)
        {
            openOption = new EventOption(
                this,
                OpenCase,
                "GOLD_GOLD_GOLD.pages.INITIAL.options.OPEN"
            );
        }
        else
        {
            openOption = new EventOption(
                this,
                null,
                "GOLD_GOLD_GOLD.pages.INITIAL.options.LOCKED"
            );
        }

        return
        [
            openOption,

            new EventOption(
                this,
                Leave,
                "GOLD_GOLD_GOLD.pages.INITIAL.options.LEAVE"
            )
        ];
    }

    private async Task OpenCase()
    {
        Godot.GD.Print("GoldGoldGold: OpenCase clicked");
        if (Owner is not Player player)
            return;

        if (player.Gold < CasePrice)
        {
            SetEventState(
                L10NLookup(
                    "GOLD_GOLD_GOLD.pages.INITIAL.description"
                ),
                GenerateInitialOptions()
            );

            return;
        }

        // Pay for the case
        await PlayerCmd.LoseGold(CasePrice, player);

        // Generate exactly one reward
        Reward reward = RollReward(player);

        // Open the native reward screen
        // The reward already exists; the animation is local presentation only.
        if (MegaCrit.Sts2.Core.Context.LocalContext.IsMe(player))
        {
            try
            {
                await GoldGoldGoldCaseRoll.PlayAsync(reward);
            }
            catch (System.Exception ex)
            {
                // Do not lose a purchased reward if the animation fails.
                Godot.GD.PushWarning($"GoldGoldGold animation: {ex}");
            }
        }

        await RewardsCmd.OfferCustom(
            player,
            new List<Reward> { reward }
        );

        // Finish event
        SetEventFinished(
            L10NLookup(
                "GOLD_GOLD_GOLD.pages.OPENED.description"
            )
        );
    }

    private Reward RollReward(Player player)
    {
        int totalWeight =
            GoldChance
            + PotionChance
            + CardChance
            + CommonRelicChance
            + RareRelicChance;

        int roll = Rng.NextInt(totalWeight);

        // Gold
        if (roll < GoldChance)
        {
            int amount = Rng.NextInt(
                MinGoldReward,
                MaxGoldReward + 1
            );

            return new GoldReward(amount, player);
        }

        roll -= GoldChance;

        // Potion
        if (roll < PotionChance)
        {
            return new PotionReward(player);
        }

        roll -= PotionChance;

        // Card
        if (roll < CardChance)
        {
            var options =
                CardCreationOptions
                    .ForNonCombatWithDefaultOdds(
                        new[] { player.Character.CardPool }
                    );

            return new CardReward(
                options,
                1,
                player
            );
        }

        roll -= CardChance;

        // Common Relic
        if (roll < CommonRelicChance)
        {
            return new RelicReward(
                RelicRarity.Common,
                player
            );
        }

        // Rare Relic
        return new RelicReward(
            RelicRarity.Rare,
            player
        );
    }

    private Task Leave()
    {
        Godot.GD.Print("GoldGoldGold: Leave clicked");
        SetEventFinished(
            L10NLookup(
                "GOLD_GOLD_GOLD.pages.LEFT.description"
            )
        );

        return Task.CompletedTask;
    }
}