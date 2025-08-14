using System.Text.Json.Serialization;

namespace BNLReloadedServer.BaseTypes;

public enum MatchType
{
    [JsonStringEnumMemberName("shield_rush_2")]
    ShieldRush2 = 1,
    ShieldCapture = 2,
    Tutorial = 3,
    TimeTrial = 4
}