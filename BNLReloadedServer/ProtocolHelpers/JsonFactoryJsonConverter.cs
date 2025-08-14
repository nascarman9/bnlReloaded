using System.Text.Json;
using System.Text.Json.Serialization;

namespace BNLReloadedServer.ProtocolHelpers;

public class JsonFactoryJsonConverter<T> : JsonConverter<T> where T : IJsonFactory<T>
{
    public override bool CanConvert(Type objectType) => typeof(T).IsAssignableFrom(objectType);
    
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) 
           return default;

        options = JsonHelper.DefaultSerializerSettings;
        var readerAtStart = reader;
        using var jsonDocument = JsonDocument.ParseValue(ref reader);
        var convClass = T.CreateFromJson(jsonDocument.RootElement);
        return (T?)JsonSerializer.Deserialize(ref readerAtStart, convClass.GetType(), options);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), JsonHelper.DefaultSerializerSettings);
    }
}