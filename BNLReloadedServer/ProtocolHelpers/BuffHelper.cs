using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.ProtocolHelpers;

public static class BuffHelper
{
  extension(Unit unit)
  {
    public float DashDistance(float baseDistance) => 
      baseDistance * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.DashDistance));

    public float DashTime(float baseTime) => 
      baseTime * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.DashTime));

    public float CofAngle(float baseAngle) => 
      Math.Max(0.0f, baseAngle - baseAngle * unit.GetBuff(BuffType.CofBonus));

    public float MagazineSize(float baseMagSize) => 
      baseMagSize * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.WeaponMagazine));

    public float PoolSize(float basePoolSize) => 
      Math.Max(Math.Min(basePoolSize, 1), basePoolSize * Math.Max(0, 1f + unit.GetBuff(BuffType.WeaponPool)));

    public float AmmoRate(float baseAmmoRate) => 
      unit.IsBuff(BuffType.InfiniteAmmo) ? 0.0f : baseAmmoRate;

    public float ReloadTime(float baseReloadTime) => 
      ApplyTimeBuff(baseReloadTime, unit.GetBuff(BuffType.WeaponReload));

    public float CrouchSpeed(float baseSpeed) => 
      Math.Max(1f, baseSpeed * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.RunSpeed)));

    public float RunSpeed(float baseSpeed) => 
      Math.Max(1f, baseSpeed * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.RunSpeed)));

    public float SprintSpeed(float baseSpeed) => 
      Math.Max(1f, baseSpeed * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.SprintSpeed)));

    public float SwimSpeed(float baseSpeed) => 
      Math.Max(1f, baseSpeed * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.SwimSpeed)));

    public float JumpHeight(float baseHeight) => 
      Math.Max(0.0f, baseHeight + unit.GetBuff(BuffType.JumpHeight));

    public float BuildCost(float buildCost) => 
      buildCost * (1 - unit.GetBuff(BuffType.BuildCostReduction));

    public float GearDropTime(float baseDropTime) => 
      ApplyTimeBuff(baseDropTime, unit.GetBuff(BuffType.WeaponSwitch));

    public float GearPickupTime(float basePickupTime) => 
      ApplyTimeBuff(basePickupTime, unit.GetBuff(BuffType.WeaponSwitch));

    public float UnitMaxHealth(float baseMaxHealth) => 
      baseMaxHealth * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.HealthCap));

    public float UnitMaxForcefield(float baseMaxForcefield) => 
      baseMaxForcefield * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.ForcefieldCap));

    public float AmmoGainAmount(float ammoGain) => 
      ammoGain * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.AmmoGain));

    public float HealthGainAmount(float healthGain) => 
      healthGain * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.HealthGain));

    public float PlayerDamageAmount(float initialDamage) =>
      initialDamage * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.PlayerDamage));

    public float WorldDamageAmount(float initialDamage) =>
      initialDamage * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.WorldDamage));

    public float ObjectiveDamageAmount(float initialDamage) =>
      initialDamage * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.ObjectiveDamage));

    public float ToolWorldDamageAmount(float initialDamage) =>
      initialDamage * Math.Max(0.0f, 1f + unit.GetBuff(BuffType.ToolWorldDamage) + unit.GetBuff(BuffType.WorldDamage));

    public float DamageTaken(float initialDamage) =>
      initialDamage * float.Max(1f - unit.GetBuff(BuffType.Shield), 0.5f);

    public float SplashDamageTaken(float initialDamage) =>
      initialDamage * Math.Max(0.0f, (100f - unit.GetBuff(BuffType.SplashDamageReduction)) / 100f) *
      float.Max(1f - unit.GetBuff(BuffType.Shield), 0.5f);

    public float AbilityCooldownTime(float initialCooldown) =>
      initialCooldown * Math.Max(0.0f, 1f - unit.GetBuff(BuffType.AbilityCooldownReduction));

    public float ResourceGainAmount(float resourceGain, ResourceType source) =>
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

    public float CombineBuffs(float originalValue, float addValue, BuffType buffType, Key effectKey) =>
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
          BuffType.WeaponMagazine or 
          BuffType.WeaponPool or 
          BuffType.WeaponReload or 
          BuffType.WeaponSwitch or 
          BuffType.CofBonus or 
          BuffType.HealthGain or 
          BuffType.AmmoGain or 
          BuffType.JumpHeight or 
          BuffType.ToolWorldDamage => float.Max(originalValue + addValue, -1),
      
        // Works like the previous list, but stacks multiplicatively if not from perks 
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
      
        // Can be -Inf -> 1, final multiplier is (1 - buff)
        BuffType.BuildCostReduction or
          BuffType.FallDamageReduction or 
          BuffType.AbilityCooldownReduction => float.Min(originalValue + addValue, 1),
        
        // Can be -Inf -> 100, final result is (100 - buff) / 100
        BuffType.SplashDamageReduction => float.Min(originalValue + addValue, 100),
      
        _ => originalValue
      };
  }

  private static float ApplyTimeBuff(float baseValue, float buffValue) => 
    buffValue <= -1.0 ? 0.0f : baseValue / (1f + buffValue);
}