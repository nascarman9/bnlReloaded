using System.Text.Json.Serialization;

namespace BNLReloadedServer.BaseTypes;

public enum MapCameraLabel
{
    Base = 1,
    [JsonStringEnumMemberName("line_1")] 
    Line1 = 2,
    [JsonStringEnumMemberName("line_2")] 
    Line2 = 3,
    [JsonStringEnumMemberName("line_3")] 
    Line3 = 4
}