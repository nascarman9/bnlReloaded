using System.Text.Json;
using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

[JsonConverter(typeof(JsonFactoryJsonConverter<LobbyMode>))]
public abstract class LobbyMode : IJsonFactory<LobbyMode>
{
    public abstract LobbyModeType Type { get; }

    public float PrestartTime { get; set; } = 10f;

    public float ReconnectSelectionTime { get; set; } = 60f;

    public static LobbyMode CreateFromJson(JsonElement json) => Create(json.GetProperty("type").Deserialize<LobbyModeType>());

    public abstract void Write(BinaryWriter writer);

    public abstract void Read(BinaryReader reader);

    public static void WriteVariant(BinaryWriter writer, LobbyMode value)
    {
        writer.WriteByteEnum(value.Type);
        value.Write(writer);
    }

    public static LobbyMode ReadVariant(BinaryReader reader)
    {
        var lobbyMode = Create(reader.ReadByteEnum<LobbyModeType>());
        lobbyMode.Read(reader);
        return lobbyMode;
    }

    public static LobbyMode Create(LobbyModeType type)
    {
        return type switch
        {
            LobbyModeType.FreePick => new LobbyModeFreePick(),
            LobbyModeType.DraftPick => new LobbyModeDraftPick(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Invalid variant tag")
        };
    }
}