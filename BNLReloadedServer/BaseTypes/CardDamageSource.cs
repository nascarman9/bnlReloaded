using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.ProtocolInterfaces;

namespace BNLReloadedServer.BaseTypes;

public class CardDamageSource : Card, IKillscoreIcon
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.DamageSource;

    public string? KillscoreIcon { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, KillscoreIcon != null).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (KillscoreIcon != null)
        writer.Write(KillscoreIcon);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      KillscoreIcon = bitField[2] ? reader.ReadString() : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardDamageSource value)
    {
      value.Write(writer);
    }

    public static CardDamageSource ReadRecord(BinaryReader reader)
    {
      var cardDamageSource = new CardDamageSource();
      cardDamageSource.Read(reader);
      return cardDamageSource;
    }
}