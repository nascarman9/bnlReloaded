using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.ProtocolInterfaces;

namespace BNLReloadedServer.BaseTypes;

public class CardLootCrate : Card, IPrefab, IIcon
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.LootCrate;

    public string? Prefab { get; set; }

    public string? Icon { get; set; }

    public LocalizedString? Name { get; set; }

    public LocalizedString? NamePlural { get; set; }

    public LootCrateRarity Rarity { get; set; } = LootCrateRarity.Common;

    public int HitsToOpen { get; set; } = 1;

    public string? ImpactPrefab { get; set; }

    public string? DestroyPrefab { get; set; }

    public string? AppearAudio { get; set; }

    public string? OpenAudio { get; set; }

    public string? HitAudio { get; set; }

    public List<LootEntry>? LootEntries { get; set; }

    public int? CustomItemCount { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, Prefab != null, Icon != null, Name != null, NamePlural != null, true, true,
        ImpactPrefab != null, DestroyPrefab != null, AppearAudio != null, OpenAudio != null, HitAudio != null,
        LootEntries != null, CustomItemCount.HasValue).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (Prefab != null)
        writer.Write(Prefab);
      if (Icon != null)
        writer.Write(Icon);
      if (Name != null)
        LocalizedString.WriteRecord(writer, Name);
      if (NamePlural != null)
        LocalizedString.WriteRecord(writer, NamePlural);
      writer.WriteByteEnum(Rarity);
      writer.Write(HitsToOpen);
      if (ImpactPrefab != null)
        writer.Write(ImpactPrefab);
      if (DestroyPrefab != null)
        writer.Write(DestroyPrefab);
      if (AppearAudio != null)
        writer.Write(AppearAudio);
      if (OpenAudio != null)
        writer.Write(OpenAudio);
      if (HitAudio != null)
        writer.Write(HitAudio);
      if (LootEntries != null)
        writer.WriteList(LootEntries, LootEntry.WriteVariant);
      if (!CustomItemCount.HasValue)
        return;
      writer.Write(CustomItemCount.Value);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(15);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      Prefab = bitField[2] ? reader.ReadString() : null;
      Icon = bitField[3] ? reader.ReadString() : null;
      Name = bitField[4] ? LocalizedString.ReadRecord(reader) : null;
      NamePlural = bitField[5] ? LocalizedString.ReadRecord(reader) : null;
      if (bitField[6])
        Rarity = reader.ReadByteEnum<LootCrateRarity>();
      if (bitField[7])
        HitsToOpen = reader.ReadInt32();
      ImpactPrefab = bitField[8] ? reader.ReadString() : null;
      DestroyPrefab = bitField[9] ? reader.ReadString() : null;
      AppearAudio = bitField[10] ? reader.ReadString() : null;
      OpenAudio = bitField[11] ? reader.ReadString() : null;
      HitAudio = bitField[12] ? reader.ReadString() : null;
      LootEntries = bitField[13] ? reader.ReadList<LootEntry, List<LootEntry>>(LootEntry.ReadVariant) : null;
      CustomItemCount = bitField[14] ? reader.ReadInt32() : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardLootCrate value) => value.Write(writer);

    public static CardLootCrate ReadRecord(BinaryReader reader)
    {
      var cardLootCrate = new CardLootCrate();
      cardLootCrate.Read(reader);
      return cardLootCrate;
    }
}