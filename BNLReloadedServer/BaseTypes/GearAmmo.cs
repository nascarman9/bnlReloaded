using BNLReloadedServer.Database;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class GearAmmo
{
    public const float AmmoCompareEpsilon = 0.001f;
    public float Mag;
    public float Pool;
    public int AmmoIndex;
    public Key GearKey;
    public Unit Unit;

    public GearAmmo(Unit unit, Key gearKey, int index)
    {
      Unit = unit;
      GearKey = gearKey;
      AmmoIndex = index;
      Mag = !IsMag ? 0.0f : MagSize;
      Pool = !IsPool ? 0.0f : PoolSize;
    }

    public CardGear GearCard => Databases.Catalogue.GetCard<CardGear>(GearKey);

    public bool IsPool => GearCard.Ammo[AmmoIndex].Pool != null;

    public bool IsMag => GearCard.Ammo[AmmoIndex].MagSize.HasValue;

    public float MagSize => BuffHelper.MagazineSize(Unit, GearCard.Ammo[AmmoIndex].MagSize.Value);

    public float PoolSize => BuffHelper.PoolSize(Unit, GearCard.Ammo[AmmoIndex].Pool.PoolSize);

    public void ServerUpdateAmmo(Ammo ammo)
    {
      if (ammo.Mag.HasValue)
        Mag = ammo.Mag.Value;
      if (!ammo.Pool.HasValue)
        return;
      Pool = ammo.Pool.Value;
    }

    public void ReloadPartial(float rate)
    {
      if (!IsMag)
        return;
      if (IsPool)
      {
        var num = Math.Min(Math.Min(rate, MagSize - Mag), Pool);
        Mag += num;
        Pool -= num;
      }
      else
        Mag = Math.Min(Mag + rate, MagSize);
    }

    public void Reload()
    {
      if (!IsMag)
        return;
      if (IsPool)
      {
        var num = Math.Min(MagSize - Mag, Pool);
        Mag += num;
        Pool -= num;
      }
      else
        Mag = MagSize;
    }

    public void TakeAmmo(float rate)
    {
      rate = BuffHelper.AmmoRate(Unit, rate);
      if (IsMag)
      {
        Mag = Math.Max(0.0f, Mag - rate);
      }
      else
      {
        if (!IsPool)
          return;
        Pool = Math.Max(0.0f, Pool - rate);
      }
    }

    public bool IsEnoughAmmoToUse(float rate)
    {
      rate = BuffHelper.AmmoRate(Unit, rate);
      return IsMag && MoreOrEqual(Mag, rate) || !IsMag && IsPool && MoreOrEqual(Pool, rate);
    }

    public bool IsOutOfAmmo(float rate)
    {
      rate = BuffHelper.AmmoRate(Unit, rate);
      return (!IsMag || !LessOrEqual(rate, Mag)) && (!IsPool || !LessOrEqual(rate, Pool));
    }

    public bool IsRequireToReload(float rate)
    {
      rate = BuffHelper.AmmoRate(Unit, rate);
      return IsMag && Less(Mag, rate);
    }

    public bool IsPossibleToReload(float rate)
    {
      rate = BuffHelper.AmmoRate(Unit, rate);
      return IsMag && Less(Mag, MagSize) && (!IsPool || IsPool && MoreOrEqual(Pool, rate));
    }

    public static bool Equal(float v1, float v2)
    {
      return v2 - (double) v1 >= 0.0 && v2 - (double) v1 <= 1.0 / 1000.0;
    }

    public static bool Less(float v1, float v2) => v2 - (double) v1 >= 1.0 / 1000.0;

    public static bool More(float v1, float v2) => v1 - (double) v2 >= 1.0 / 1000.0;

    public static bool LessOrEqual(float v1, float v2)
    {
      return Less(v1, v2) || Equal(v1, v2);
    }

    public static bool MoreOrEqual(float v1, float v2)
    {
      return More(v1, v2) || Equal(v1, v2);
    }
}