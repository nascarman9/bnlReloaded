using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CardSettingsLogic : Card
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.SettingsLogic;

    public Dictionary<string, GraphicsLevelPreset>? DefaultSettings { get; set; }

    public Dictionary<GraphicsLevelPreset, Vector2s>? DefaultResolution { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, DefaultSettings != null, DefaultResolution != null).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (DefaultSettings != null)
        writer.WriteMap(DefaultSettings, writer.Write, writer.WriteByteEnum);
      if (DefaultResolution != null)
        writer.WriteMap(DefaultResolution, writer.WriteByteEnum, writer.Write);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      DefaultSettings = bitField[2] ? reader.ReadMap<string, GraphicsLevelPreset, Dictionary<string, GraphicsLevelPreset>>(reader.ReadString, reader.ReadByteEnum<GraphicsLevelPreset>) : null;
      DefaultResolution = bitField[3] ? reader.ReadMap<GraphicsLevelPreset, Vector2s, Dictionary<GraphicsLevelPreset, Vector2s>>(reader.ReadByteEnum<GraphicsLevelPreset>, reader.ReadVector2s) : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardSettingsLogic value)
    {
      value.Write(writer);
    }

    public static CardSettingsLogic ReadRecord(BinaryReader reader)
    {
      var cardSettingsLogic = new CardSettingsLogic();
      cardSettingsLogic.Read(reader);
      return cardSettingsLogic;
    }
}