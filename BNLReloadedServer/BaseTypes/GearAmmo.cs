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

    public bool IsPool => GearCard.Ammo?[AmmoIndex].Pool != null;

    public bool IsMag => GearCard.Ammo?[AmmoIndex].MagSize != null;

    public float MagSize => Unit.MagazineSize(GearCard.Ammo[AmmoIndex].MagSize.Value);

    public float PoolSize => Unit.PoolSize(GearCard.Ammo[AmmoIndex].Pool.PoolSize);

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
    
    public Ammo? ReloadPartialAmmo(float rate)
    {
      if (!IsMag)
        return null;
      
      var ammo = new Ammo
      {
        Index = AmmoIndex,
        Mag = Mag,
        Pool = Pool
      };
      
      if (IsPool)
      {
        var num = Math.Min(Math.Min(rate, MagSize - ammo.Mag.Value), ammo.Pool.Value);
        ammo.Mag += num;
        ammo.Pool -= num;
      }
      else
        ammo.Mag = Math.Min(Mag + rate, MagSize);
      
      return ammo;
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

    public Ammo? ReloadAmmo()
    {
      if (!IsMag)
        return null;
      
      var ammo = new Ammo
      {
        Index = AmmoIndex,
        Mag = Mag,
        Pool = Pool
      };
      
      if (IsPool)
      {
        var num = Math.Min(MagSize - ammo.Mag.Value, ammo.Pool.Value);
        ammo.Mag += num;
        ammo.Pool -= num;
      }
      else
        ammo.Mag = MagSize;
      
      return ammo;
    }

    public void TakeAmmo(float rate)
    {
      rate = Unit.AmmoRate(rate);
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

    public Ammo? TakeAmmoUpdate(float rate)
    {
      rate = Unit.AmmoRate(rate);
      
      var ammo = new Ammo
      {
        Index = AmmoIndex,
        Mag = Mag,
        Pool = Pool
      };
      
      if (IsMag)
      {
        ammo.Mag = Math.Max(0.0f, ammo.Mag.Value - rate);
      }
      else
      {
        if (!IsPool)
          return null;
        ammo.Pool = Math.Max(0.0f, ammo.Pool.Value - rate);
      }
      
      return ammo;
    }

    public bool IsEnoughAmmoToUse(float rate)
    {
      rate = Unit.AmmoRate(rate);
      return IsMag && MoreOrEqual(Mag, rate) || !IsMag && IsPool && MoreOrEqual(Pool, rate);
    }

    public bool IsOutOfAmmo(float rate)
    {
      rate = Unit.AmmoRate(rate);
      return (!IsMag || !LessOrEqual(rate, Mag)) && (!IsPool || !LessOrEqual(rate, Pool));
    }

    public bool IsRequireToReload(float rate)
    {
      rate = Unit.AmmoRate(rate);
      return IsMag && Less(Mag, rate);
    }

    public bool IsPossibleToReload(float rate)
    {
      rate = Unit.AmmoRate(rate);
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