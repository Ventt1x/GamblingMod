
using MegaCrit.Sts2.Core.Entities.Powers;
using ModSmith.Models;

namespace GamblingMod.Powers;

public sealed class LuckPower : ModSmithPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType =>
        PowerStackType.Counter;

    public override bool AllowNegative => false;

    // Kleines Symbol über der Lebensanzeige
    protected override string PackedIconPath =>
        "res://GamblingMod/Assets/Powers/luck.png";

    // Großes Symbol in der Statusbeschreibung
    protected override string BigIconPath =>
        "res://GamblingMod/Assets/Powers/luck.png";
}
