namespace BNLReloadedServer.BaseTypes;

public enum ConstEffectType
{
    Buff = 1,
    Immunity = 2,
    Aura = 3,
    Self = 4,
    Team = 5,
    OnSprint = 6,
    OnDeath = 7,
    OnFall = 8,
    OnKill = 9,
    OnBuilding = 10, // 0x0000000A
    OnLowHealth = 11, // 0x0000000B
    OnLeading = 12, // 0x0000000C
    OnMatchContext = 13, // 0x0000000D
    OnDamageTaken = 14, // 0x0000000E
    OnGearSwitch = 15, // 0x0000000F
    Pull = 16, // 0x00000010
    OnReload = 17, // 0x00000011
    Interval = 18, // 0x00000012
    OnNearbyBlock = 19 // 0x00000013
}