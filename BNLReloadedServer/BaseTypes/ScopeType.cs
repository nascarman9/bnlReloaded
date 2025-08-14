using System.Text.Json.Serialization;

namespace BNLReloadedServer.BaseTypes;

[JsonConverter(typeof(JsonStringEnumConverter<ScopeType>))]
public enum ScopeType
{
    Public = 1,
    Private = 2
}