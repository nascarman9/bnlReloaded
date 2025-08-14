using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CardMovementLogic : Card
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.MovementLogic;

    public float JumpForwardAcceleration { get; set; } = 15f;

    public float JumpStrafeAcceleration { get; set; } = 10f;

    public float? JumpAirResistance { get; set; }

    public float GroundForwardAcceleration { get; set; } = 10f;

    public float GroundStrafeAcceleration { get; set; } = 10f;

    public float GroundFriction { get; set; } = 10f;

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, true, true, JumpAirResistance.HasValue, true, true, true).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      writer.Write(JumpForwardAcceleration);
      writer.Write(JumpStrafeAcceleration);
      if (JumpAirResistance.HasValue)
        writer.Write(JumpAirResistance.Value);
      writer.Write(GroundForwardAcceleration);
      writer.Write(GroundStrafeAcceleration);
      writer.Write(GroundFriction);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(8);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      if (bitField[2])
        JumpForwardAcceleration = reader.ReadSingle();
      if (bitField[3])
        JumpStrafeAcceleration = reader.ReadSingle();
      JumpAirResistance = bitField[4] ? reader.ReadSingle() : null;
      if (bitField[5])
        GroundForwardAcceleration = reader.ReadSingle();
      if (bitField[6])
        GroundStrafeAcceleration = reader.ReadSingle();
      if (!bitField[7])
        return;
      GroundFriction = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, CardMovementLogic value)
    {
      value.Write(writer);
    }

    public static CardMovementLogic ReadRecord(BinaryReader reader)
    {
      var cardMovementLogic = new CardMovementLogic();
      cardMovementLogic.Read(reader);
      return cardMovementLogic;
    }
}