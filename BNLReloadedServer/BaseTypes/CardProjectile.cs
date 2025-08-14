using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.ProtocolInterfaces;

namespace BNLReloadedServer.BaseTypes;

public class CardProjectile : Card, IPrefab
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.Projectile;

    public string? Prefab { get; set; }

    public ProjectileBehaviour? Behaviour { get; set; }

    public ProjectileVisualAttachment? PlayerAttachment { get; set; }

    public ProjectileVisualAttachment? UnitAttachment { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, Prefab != null, Behaviour != null, PlayerAttachment != null, UnitAttachment != null).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (Prefab != null)
        writer.Write(Prefab);
      if (Behaviour != null)
        ProjectileBehaviour.WriteVariant(writer, Behaviour);
      if (PlayerAttachment != null)
        ProjectileVisualAttachment.WriteRecord(writer, PlayerAttachment);
      if (UnitAttachment == null)
        return;
      ProjectileVisualAttachment.WriteRecord(writer, UnitAttachment);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(6);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      Prefab = bitField[2] ? reader.ReadString() : null;
      Behaviour = bitField[3] ? ProjectileBehaviour.ReadVariant(reader) : null;
      PlayerAttachment = bitField[4] ? ProjectileVisualAttachment.ReadRecord(reader) : null;
      UnitAttachment = bitField[5] ? ProjectileVisualAttachment.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardProjectile value)
    {
      value.Write(writer);
    }

    public static CardProjectile ReadRecord(BinaryReader reader)
    {
      var cardProjectile = new CardProjectile();
      cardProjectile.Read(reader);
      return cardProjectile;
    }
}