using MegaCrit.Sts2.Core.Models.CardPools;
using ModSmith.Registry;

namespace GamblingMod;

public static class GamblingContent
{
  public static void RegisterContent()
  {
    Registry.RegisterCard<RepeatStrike, IroncladCardPool>();
  }
}