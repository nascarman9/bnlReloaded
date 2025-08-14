using System.Text.Json.Serialization;

namespace BNLReloadedServer.BaseTypes;

public enum UnitLabel
{
    Objective = 1,
    ShieldGenerator = 2,
    ShieldGeneratorDestroyed = 3,
    DropPointResource = 4,
    DropPointBlockbuster = 5,
    DropPointBase = 6,
    SupplyResource = 7,
    SupplyBlockbuster = 8,
    Base = 9,
    BaseTutorial = 10, // 0x0000000A
    [JsonStringEnumMemberName("line_1")]
    Line1 = 11, // 0x0000000B
    [JsonStringEnumMemberName("line_2")]
    Line2 = 12, // 0x0000000C
    [JsonStringEnumMemberName("line_3")]
    Line3 = 13, // 0x0000000D
    LineBase = 14, // 0x0000000E
    NinjaNest = 15, // 0x0000000F
    Repairable = 16, // 0x00000010
    [JsonStringEnumMemberName("srv2_objective_1")]
    Srv2Objective1 = 17, // 0x00000011
    [JsonStringEnumMemberName("srv2_objective_2")]
    Srv2Objective2 = 18, // 0x00000012
    HealthSupply = 19, // 0x00000013
    AmmmoSupply = 20, // 0x00000014
    RespawnPoint = 21, // 0x00000015
    NoBuildZone = 22, // 0x00000016
    AstroDisc = 23, // 0x00000017
    TutorialCheckpoint = 24, // 0x00000018
    DestroyOnMatchEnd = 25, // 0x00000019
    EngineerTurret = 26, // 0x0000001A
    ObjectiveCapturer = 27, // 0x0000001B
    IgnoreFriendlyFireCrosshair = 28, // 0x0000001C
    PlayerDamageSource = 29, // 0x0000001D
    DisabledInBuildPhase = 30 // 0x0000001E
}