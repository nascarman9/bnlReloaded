using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CardVibrationLogic : Card
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.VibrationLogic;

    public Dictionary<string, List<VibrationData>>? Vibrations { get; set; }

    public Dictionary<string, ExplosionData>? ExplosionVibrations { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, Vibrations != null, ExplosionVibrations != null).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (Vibrations != null)
        writer.WriteMap(Vibrations, writer.Write, item => writer.WriteList(item, VibrationData.WriteRecord));
      if (ExplosionVibrations != null)
        writer.WriteMap(ExplosionVibrations, writer.Write, ExplosionData.WriteRecord);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      Vibrations = bitField[2] ? reader.ReadMap<string, List<VibrationData>, Dictionary<string, List<VibrationData>>>(reader.ReadString, () => reader.ReadList<VibrationData, List<VibrationData>>(VibrationData.ReadRecord)) : null;
      ExplosionVibrations = bitField[3] ? reader.ReadMap<string, ExplosionData, Dictionary<string, ExplosionData>>(reader.ReadString, ExplosionData.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardVibrationLogic value)
    {
      value.Write(writer);
    }

    public static CardVibrationLogic ReadRecord(BinaryReader reader)
    {
      var cardVibrationLogic = new CardVibrationLogic();
      cardVibrationLogic.Read(reader);
      return cardVibrationLogic;
    }
}