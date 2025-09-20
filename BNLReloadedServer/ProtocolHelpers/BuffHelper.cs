using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.ProtocolHelpers;

public static class BuffHelper
{
  public static float DashDistance(this Unit unit, float baseDistance) => 
    baseDistance * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.DashDistance));

  public static float DashTime(this Unit unit, float baseTime) => 
    baseTime * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.DashTime));

  public static float CofAngle(this Unit unit, float baseAngle) => 
    Math.Max(0.0f, baseAngle - baseAngle * unit.GetBuff(BuffType.CofBonus));

  public static float MagazineSize(this Unit unit, float baseMagSize) => 
    baseMagSize * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.WeaponMagazine));

  public static float PoolSize(this Unit unit, float basePoolSize) => 
    basePoolSize * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.WeaponPool));

  public static float AmmoRate(this Unit unit, float baseAmmoRate) => 
    unit.IsBuff(BuffType.InfiniteAmmo) ? 0.0f : baseAmmoRate;

  public static float ReloadTime(this Unit unit, float baseReloadTime) => 
    ApplyTimeBuff(baseReloadTime, unit.GetBuff(BuffType.WeaponReload));

  public static float CrouchSpeed(this Unit unit, float baseSpeed) => 
    Math.Max(1f, baseSpeed * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.RunSpeed)));

  public static float RunSpeed(this Unit unit, float baseSpeed) => 
    Math.Max(1f, baseSpeed * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.RunSpeed)));

  public static float SprintSpeed(this Unit unit, float baseSpeed) => 
    Math.Max(1f, baseSpeed * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.SprintSpeed)));

  public static float SwimSpeed(this Unit unit, float baseSpeed) => 
    Math.Max(1f, baseSpeed * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.SwimSpeed)));

  public static float JumpHeight(this Unit unit, float baseHeight) => 
    Math.Max(0.0f, baseHeight + unit.GetBuff(BuffType.JumpHeight));

  public static float BuildCost(this Unit unit, float buildCost) => 
    buildCost * (1 - unit.GetBuff(BuffType.BuildCostReduction));

  public static float GearDropTime(this Unit unit, float baseDropTime) => 
    ApplyTimeBuff(baseDropTime, unit.GetBuff(BuffType.WeaponSwitch));

  public static float GearPickupTime(this Unit unit, float basePickupTime) => 
    ApplyTimeBuff(basePickupTime, unit.GetBuff(BuffType.WeaponSwitch));

  public static float UnitMaxHealth(this Unit unit, float baseMaxHealth) => 
    baseMaxHealth * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.HealthCap));

  public static float UnitMaxForcefield(this Unit unit, float baseMaxForcefield) => 
    baseMaxForcefield * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.ForcefieldCap));

  public static float AmmoGainAmount(this Unit unit, float ammoGain) => 
    ammoGain * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.AmmoGain));

  public static float HealthGainAmount(this Unit unit, float healthGain) => 
    healthGain * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.HealthGain));

  public static float ResourceGainAmount(this Unit unit, float resourceGain, ResourceType source) =>
    source switch
    {
      ResourceType.General => resourceGain * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.ResourceBonus)),
      
      ResourceType.Mining => resourceGain * Math.Max(0.0f,
        1f + unit.GetBuff(BuffType.ResourceBonus) + unit.GetBuff(BuffType.MiningBonus)),
      
      ResourceType.Kill => resourceGain * Math.Max(0.0f,
        1f + unit.GetBuff(BuffType.ResourceBonus) + unit.GetBuff(BuffType.KillResourceBonus)),
      
      ResourceType.TeamKill => resourceGain * Math.Max(0.0f,
        1f + unit.GetBuff(BuffType.ResourceBonus) + unit.GetBuff(BuffType.KillByTeamResourceBonus)),
      
      ResourceType.Supply => resourceGain * Math.Max(0.0f,
        1f + unit.GetBuff(BuffType.ResourceBonus) + unit.GetBuff(BuffType.SupplyResourceBonus)),
      
      ResourceType.Objective => resourceGain * Math.Max(0.0f,
        1f + unit.GetBuff(BuffType.ResourceBonus) + unit.GetBuff(BuffType.ObjectiveResourceBonus)),
      
      _ => resourceGain
    };

  private static float ApplyTimeBuff(float baseValue, float buffValue) => 
    buffValue <= -1.0 ? 0.0f : baseValue / (1f + buffValue);

  public static float CombineBuffs(this Unit unit, float originalValue, float addValue, BuffType buffType, Key effectKey) =>
    buffType switch
    {
      // Can be 0 -> Inf, final total is the buff amount
      BuffType.ResourceProduction or 
        BuffType.HealthRegen or 
        BuffType.ForcefieldRegen or 
        BuffType.AmmoRegen or 
        BuffType.WallClimb or 
        BuffType.MiningAmmoRefill or 
        BuffType.MiningHealthRefill or 
        BuffType.SupplyForcefield or 
        BuffType.SupplyAmmo or 
        BuffType.SupplyHealth => float.Max(originalValue + addValue, 0),
      
      // Works like the previous list, but doesn't stack  
        BuffType.Bleeding or 
        BuffType.Burning or 
        BuffType.Poisoned or 
        BuffType.Decay or 
        BuffType.Sway or 
        BuffType.Disabled => float.Max(originalValue, addValue),
      
      // Can be -1 -> Inf, final multiplier is (1 + buff)  
      BuffType.ResourceBonus or 
        BuffType.KillResourceBonus or 
        BuffType.KillByTeamResourceBonus or 
        BuffType.SupplyResourceBonus or 
        BuffType.ObjectiveResourceBonus or 
        BuffType.MiningBonus or 
        BuffType.HealthCap or 
        BuffType.ForcefieldCap or 
        BuffType.AmmoDrain or 
        BuffType.PlayerDamage or 
        BuffType.WorldDamage or 
        BuffType.ObjectiveDamage or 
        BuffType.BuildCostReduction or 
        BuffType.WeaponMagazine or 
        BuffType.WeaponPool or 
        BuffType.WeaponReload or 
        BuffType.WeaponSwitch or 
        BuffType.FallDamageReduction or 
        BuffType.CofBonus or 
        BuffType.AbilityCooldownReduction or 
        BuffType.HealthGain or 
        BuffType.AmmoGain or 
        BuffType.ToolWorldDamage => float.Max(originalValue + addValue, -1),
      
      // Works like the previous list, but stacks multiplicatively if not from perks 
      BuffType.JumpHeight or 
        BuffType.RunSpeed or 
        BuffType.SprintSpeed or 
        BuffType.SwimSpeed or 
        BuffType.BuildSpeed or 
        BuffType.DashTime or 
        BuffType.DashDistance => unit.InitialEffects.Count == 0 || unit.InitialEffects.ContainsKey(effectKey)
          ? float.Max(originalValue + addValue, -1)
          : float.Max(float.FusedMultiplyAdd(1 + originalValue, 1 + addValue, -1), -1),
      
      // Works like the previous list, but doesn't stack
      BuffType.Shield => float.Max(originalValue, addValue),
        
      // 1 if active  
      BuffType.VisionMark or 
        BuffType.Invulnerability or 
        BuffType.Root or 
        BuffType.Disarm or 
        BuffType.Confusion or 
        BuffType.InfiniteAmmo or 
        BuffType.SlipperyImmunity or 
        BuffType.KnockbackIgnore => 1,
      
      // Can be -Inf -> 100, final result is (100 - buff) / 100
      BuffType.SplashDamageReduction => float.Min(originalValue + addValue, 100),
      
      _ => originalValue
    };
}