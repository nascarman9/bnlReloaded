using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace BNLReloadedServer.ProtocolHelpers;

public static class JsonHelper
{
    public static readonly JsonSerializerOptions DefaultSerializerSettings;

    static JsonHelper()
    {
        DefaultSerializerSettings = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };
        
        DefaultSerializerSettings.Converters.Add(new KeyJsonConverter());
        DefaultSerializerSettings.Converters.Add(new ColorJsonConverter());
        DefaultSerializerSettings.Converters.Add(new ColorFloatJsonConverter());
        DefaultSerializerSettings.Converters.Add(new DateTimeJsonConverter());
        DefaultSerializerSettings.Converters.Add(new QuaternionJsonConverter());
        DefaultSerializerSettings.Converters.Add(new Vector2JsonConverter());
        DefaultSerializerSettings.Converters.Add(new Vector2sJsonConverter());
        DefaultSerializerSettings.Converters.Add(new Vector3JsonConverter());
        DefaultSerializerSettings.Converters.Add(new Vector3sJsonConverter());
        DefaultSerializerSettings.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower, false));
    }
    
    public static T? Deserialize<T>(this string json)
    {       
        return JsonSerializer.Deserialize<T>(json, DefaultSerializerSettings);
    }

    public static T? Deserialize<T>(this JsonElement json)
    {
        return json.Deserialize<T>(DefaultSerializerSettings);
    }
}