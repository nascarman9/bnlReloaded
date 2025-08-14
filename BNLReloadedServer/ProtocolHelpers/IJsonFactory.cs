using System.Text.Json;

namespace BNLReloadedServer.ProtocolHelpers;

public interface IJsonFactory<out T>
{
    public static abstract T CreateFromJson(JsonElement json);
}