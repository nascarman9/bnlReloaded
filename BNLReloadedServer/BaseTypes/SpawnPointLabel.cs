using System.Text.Json.Serialization;

namespace BNLReloadedServer.BaseTypes;

public enum SpawnPointLabel
{
    Base = 1,
    [JsonStringEnumMemberName("objective_1")] 
    Objective1 = 2
}