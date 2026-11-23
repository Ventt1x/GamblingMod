
using MegaCrit.Sts2.Core.Models.CardPools;
using ModSmith.Registry;
using GamblingMod.Powers;

namespace GamblingMod;

public static class GamblingContent
{
    public static void RegisterContent()
    {
        // Eigene Statuseffekte
        Registry.RegisterPower<LuckPower>();

        // Eigene Karten
        Registry.RegisterCard<RepeatStrike, IroncladCardPool>();
        Registry.RegisterCard<RussianRoulette, IroncladCardPool>();
        Registry.RegisterCard<LuckyDay, IroncladCardPool>();
        Registry.RegisterCard<GambleTogether, ColorlessCardPool>();
        Registry.RegisterCard<FortuneShield, IroncladCardPool>();
    }
}
