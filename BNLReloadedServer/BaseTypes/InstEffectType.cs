namespace BNLReloadedServer.BaseTypes;

public enum InstEffectType
{
    Damage = 1,
    SplashDamage = 2,
    Heal = 3,
    Supply = 4,
    AddAmmo = 5,
    AddAmmoPercent = 6,
    DrainAmmo = 7,
    DrainMagazineAmmo = 8,
    AddResource = 9,
    UnitSpawn = 10, // 0x0000000A
    BlocksSpawn = 11, // 0x0000000B
    BuildDevice = 12, // 0x0000000C
    Bunch = 13, // 0x0000000D
    AllUnitsBunch = 14, // 0x0000000E
    CasterBunch = 15, // 0x0000000F
    HealBlocks = 16, // 0x00000010
    DamageBlocks = 17, // 0x00000011
    Teleport = 18, // 0x00000012
    TeleportTo = 19, // 0x00000013
    FireMortars = 20, // 0x00000014
    Knockback = 21, // 0x00000015
    Purge = 22, // 0x00000016
    InstReload = 23, // 0x00000017
    Kill = 24, // 0x00000018
    ChargeTesla = 25, // 0x00000019
    AllPlayersPersistent = 26, // 0x0000001A
    ResourceAll = 27, // 0x0000001B
    ZoneEffect = 28, // 0x0000001C
    Slip = 29, // 0x0000001D
    ReplaceBlocks = 30 // 0x0000001E
}