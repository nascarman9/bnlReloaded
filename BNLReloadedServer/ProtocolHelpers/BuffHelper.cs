using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.ProtocolHelpers;

public static class BuffHelper
{
  public static float DashDistance(Unit unit, float baseDistance)
  {
    return baseDistance * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.DashDistance));
  }

  public static float DashTime(Unit unit, float baseTime)
  {
    return baseTime * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.DashTime));
  }

  public static float CofAngle(Unit unit, float baseAngle)
  {
    return Math.Max(0.0f, baseAngle - baseAngle * unit.GetBuff(BuffType.CofBonus));
  }

  public static float MagazineSize(Unit unit, float baseMagSize)
  {
    return baseMagSize * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.WeaponMagazine));
  }

  public static float PoolSize(Unit unit, float basePoolSize)
  {
    return basePoolSize * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.WeaponPool));
  }

  public static float AmmoRate(Unit unit, float baseAmmoRate)
  {
    return unit.IsBuff(BuffType.InfiniteAmmo) ? 0.0f : baseAmmoRate;
  }

  public static float ReloadTime(Unit unit, float baseReloadTime)
  {
    return ApplyTimeBuff(baseReloadTime, unit.GetBuff(BuffType.WeaponReload));
  }

  public static float CrouchSpeed(Unit unit, float baseSpeed)
  {
    return Math.Max(1f, baseSpeed * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.RunSpeed)));
  }

  public static float RunSpeed(Unit unit, float baseSpeed)
  {
    return Math.Max(1f, baseSpeed * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.RunSpeed)));
  }

  public static float SprintSpeed(Unit unit, float baseSpeed)
  {
    return Math.Max(1f, baseSpeed * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.SprintSpeed)));
  }

  public static float SwimSpeed(Unit unit, float baseSpeed)
  {
    return Math.Max(1f, baseSpeed * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.SwimSpeed)));
  }

  public static float JumpHeight(Unit unit, float baseHeight)
  {
    return Math.Max(0.0f, baseHeight + unit.GetBuff(BuffType.JumpHeight));
  }
  
  public static float BuildCost(Unit unit, float buildCost)
  {
    return buildCost * (1 - unit.GetBuff(BuffType.BuildCostReduction));
  }

  public static float GearDropTime(Unit unit, float baseDropTime)
  {
    return ApplyTimeBuff(baseDropTime, unit.GetBuff(BuffType.WeaponSwitch));
  }

  public static float GearPickupTime(Unit unit, float basePickupTime)
  {
    return ApplyTimeBuff(basePickupTime, unit.GetBuff(BuffType.WeaponSwitch));
  }

  public static float UnitMaxHealth(Unit unit, float baseMaxHealth)
  {
    return baseMaxHealth * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.HealthCap));
  }

  public static float UnitMaxForcefield(Unit unit, float baseMaxForcefield)
  {
    return baseMaxForcefield * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.ForcefieldCap));
  }

  private static float ApplyTimeBuff(float baseValue, float buffValue)
  {
    return buffValue <= -1.0 ? 0.0f : baseValue / (1f + buffValue);
  }
}