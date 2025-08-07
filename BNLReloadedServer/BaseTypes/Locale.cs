using System.Text.Json.Serialization;

namespace BNLReloadedServer.BaseTypes;

[JsonConverter(typeof(JsonStringEnumConverter<Locale>))]
public enum Locale
{
    en = 1,
    es = 2,
    ru = 3,
    de = 4,
    fr = 5,
    pt = 6,
}