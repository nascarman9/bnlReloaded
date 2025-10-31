using System.Numerics;
using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.ServerTypes;

public record DamageData(
    float SelfDamage,
    float FriendDamage,
    float EnemyDamage,
    float TeamDeviceDamage,
    float EnemyDeviceDamage,
    float BlockDamage,
    float TeamObjectiveDamage,
    float EnemyObjectiveDamage,
    bool Mining,
    bool Melee,
    bool IgnoreInvincibility,
    bool IgnoreDefences)
{

    public DamageData ReduceBy(float amount) =>
        this with
        {
            SelfDamage = float.Max(SelfDamage - amount, 0),
            FriendDamage = float.Max(FriendDamage - amount, 0),
            EnemyDamage = float.Max(EnemyDamage - amount, 0),
            TeamDeviceDamage = float.Max(TeamDeviceDamage - amount, 0),
            EnemyDeviceDamage = float.Max(EnemyDeviceDamage - amount, 0),
            BlockDamage = float.Max(BlockDamage - amount, 0),
            TeamObjectiveDamage = float.Max(TeamObjectiveDamage - amount, 0),
            EnemyObjectiveDamage = float.Max(EnemyObjectiveDamage - amount, 0)
        };
    
    public DamageData ReduceByPercent(float amount)
    {
        var reductionAmt = float.Max(1 - amount, 0);
        return this with
        {
            SelfDamage = SelfDamage * reductionAmt,
            FriendDamage = FriendDamage * reductionAmt,
            EnemyDamage = EnemyDamage * reductionAmt,
            TeamDeviceDamage = TeamDeviceDamage * reductionAmt,
            EnemyDeviceDamage = EnemyDeviceDamage * reductionAmt,
            BlockDamage = BlockDamage * reductionAmt,
            TeamObjectiveDamage = TeamObjectiveDamage * reductionAmt,
            EnemyObjectiveDamage = EnemyObjectiveDamage * reductionAmt
        };
    }
    
    public DamageData ReduceByPercent(float unitAmount, float blockAmount)
    {
        var reductionUnitAmt = float.Max(1 - unitAmount, 0);
        var reductionBlockAmt = float.Max(1 - blockAmount, 0);
        return this with
        {
            SelfDamage = SelfDamage * reductionUnitAmt,
            FriendDamage = FriendDamage * reductionUnitAmt,
            EnemyDamage = EnemyDamage * reductionUnitAmt,
            TeamDeviceDamage = TeamDeviceDamage * reductionUnitAmt,
            EnemyDeviceDamage = EnemyDeviceDamage * reductionUnitAmt,
            BlockDamage = BlockDamage * reductionBlockAmt,
            TeamObjectiveDamage = TeamObjectiveDamage * reductionUnitAmt,
            EnemyObjectiveDamage = EnemyObjectiveDamage * reductionUnitAmt
        };
    }

    public bool IsZeroDamage() => SelfDamage == 0 && FriendDamage == 0 && EnemyDamage == 0 && TeamDeviceDamage == 0 &&
                                  EnemyDeviceDamage == 0 && BlockDamage == 0 && TeamObjectiveDamage == 0 &&
                                  EnemyObjectiveDamage == 0;

    public static DamageData ZeroDamage => new(0,
        0,
        0,
        0,
        0,
        0,
        0,
        0,
        false,
        false,
        false,
        false);
}