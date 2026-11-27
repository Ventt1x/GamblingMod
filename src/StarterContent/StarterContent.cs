using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.CardPools;
using ModSmith.Registry;

namespace GamblingMod;

public static class StarterContent
{
  public static void RegisterStarterContent()
  {
        global::GamblingMod.CharacterRegistrationPatch.Install();
    Registry.RegisterPotion<DropOfGold>();
    Registry.RegisterCard<CoinFlip, global::GamblingMod.TalentedGamblerCardPool>();
    Registry.RegisterEvent<GoldGoldGold>();
    Registry.RegisterCard<FortuneShield, global::GamblingMod.TalentedGamblerCardPool>();
    Registry.RegisterCard<GambleTogether, global::GamblingMod.TalentedGamblerCardPool>();
    Registry.RegisterCard<LuckyDay, global::GamblingMod.TalentedGamblerCardPool>();
    Registry.RegisterCard<RussianRoulette, global::GamblingMod.TalentedGamblerCardPool>();
    Registry.RegisterCard<RepeatStrike, global::GamblingMod.TalentedGamblerCardPool>();
    Registry.RegisterRelic<GoldArmor>();
    //Registry.RegisterPotion<GoldPaint>();
    //Registry.RegisterPower<MadeOfGold>();
    Registry.RegisterEvent<TheGoldCoinRoom>();
    Registry.RegisterAncientEvent<GoldGuy, Hive>();
  }
}

