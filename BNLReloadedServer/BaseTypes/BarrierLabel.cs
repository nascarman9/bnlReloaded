using System.Text.Json.Serialization;

namespace BNLReloadedServer.BaseTypes;

public enum BarrierLabel
{
    [JsonStringEnumMemberName("build_1_team_1")] 
    Build1Team1 = 1,
    [JsonStringEnumMemberName("build_1_team_2")] 
    Build1Team2 = 2,
    [JsonStringEnumMemberName("build_2_team_1")] 
    Build2Team1 = 3,
    [JsonStringEnumMemberName("build_2_team_2")] 
    Build2Team2 = 4
}