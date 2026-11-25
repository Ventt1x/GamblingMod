using System;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace GamblingMod;

public sealed class TalentedGamblerCardPool : CardPoolModel
{
    public override string Title => "talented_gambler";
    public override string EnergyColorName => "ironclad";
    public override string CardFrameMaterialPath => "card_frame_red";
    public override Color DeckEntryCardColor => new Color("D62000");
    public override Color EnergyOutlineColor => new Color("802020");
    public override bool IsColorless => false;

    // ModSmith registers the Gambling cards into this pool.
    protected override CardModel[] GenerateAllCards() => Array.Empty<CardModel>();
}
