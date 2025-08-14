using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CardImpact : Card
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.Impact;

    public string? HitPrefabForUnit { get; set; }

    public string? HitPrefabForPlayer { get; set; }

    public SoundReference? HitSound { get; set; }

    public SoundReference? HitForPlayerSound { get; set; }

    public SoundReference? HitForUnitSound { get; set; }

    public ImpactType ImpactType { get; set; }

    public bool AffectMaterial { get; set; }

    public bool Positive { get; set; }

    public bool IgnoreWrongDamageSound { get; set; }

    public ExplosionData? ExplosionData { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, HitPrefabForUnit != null, HitPrefabForPlayer != null, HitSound != null,
        HitForPlayerSound != null, HitForUnitSound != null, true, true, true, true,
        ExplosionData != null).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (HitPrefabForUnit != null)
        writer.Write(HitPrefabForUnit);
      if (HitPrefabForPlayer != null)
        writer.Write(HitPrefabForPlayer);
      if (HitSound != null)
        SoundReference.WriteRecord(writer, HitSound);
      if (HitForPlayerSound != null)
        SoundReference.WriteRecord(writer, HitForPlayerSound);
      if (HitForUnitSound != null)
        SoundReference.WriteRecord(writer, HitForUnitSound);
      writer.WriteByteEnum(ImpactType);
      writer.Write(AffectMaterial);
      writer.Write(Positive);
      writer.Write(IgnoreWrongDamageSound);
      if (ExplosionData == null)
        return;
      ExplosionData.WriteRecord(writer, ExplosionData);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(12);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      HitPrefabForUnit = bitField[2] ? reader.ReadString() : null;
      HitPrefabForPlayer = bitField[3] ? reader.ReadString() : null;
      HitSound = bitField[4] ? SoundReference.ReadRecord(reader) : null;
      HitForPlayerSound = bitField[5] ? SoundReference.ReadRecord(reader) : null;
      HitForUnitSound = bitField[6] ? SoundReference.ReadRecord(reader) : null;
      if (bitField[7])
        ImpactType = reader.ReadByteEnum<ImpactType>();
      if (bitField[8])
        AffectMaterial = reader.ReadBoolean();
      if (bitField[9])
        Positive = reader.ReadBoolean();
      if (bitField[10])
        IgnoreWrongDamageSound = reader.ReadBoolean();
      ExplosionData = bitField[11] ? ExplosionData.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardImpact value) => value.Write(writer);

    public static CardImpact ReadRecord(BinaryReader reader)
    {
      var cardImpact = new CardImpact();
      cardImpact.Read(reader);
      return cardImpact;
    }
}