using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.ProtocolInterfaces;

namespace BNLReloadedServer.BaseTypes;

public class CardPerk : Card, IIcon
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.Perk;

    public string? Icon { get; set; }

    public LocalizedString? Name { get; set; }

    public LocalizedString? Description { get; set; }

    [JsonPropertyName("description_2")]
    public LocalizedString? Description2 { get; set; }

    public string? Label { get; set; }

    public int Level { get; set; }

    public PerkSlotType SlotType { get; set; }

    public Key? HeroDependency { get; set; }

    public List<PerkMod>? PerkMods { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, Icon != null, Name != null, Description != null, Description2 != null,
        Label != null, true, true, HeroDependency.HasValue, PerkMods != null).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (Icon != null)
        writer.Write(Icon);
      if (Name != null)
        LocalizedString.WriteRecord(writer, Name);
      if (Description != null)
        LocalizedString.WriteRecord(writer, Description);
      if (Description2 != null)
        LocalizedString.WriteRecord(writer, Description2);
      if (Label != null)
        writer.Write(Label);
      writer.Write(Level);
      writer.WriteByteEnum(SlotType);
      if (HeroDependency.HasValue)
        Key.WriteRecord(writer, HeroDependency.Value);
      if (PerkMods != null)
        writer.WriteList(PerkMods, PerkMod.WriteVariant);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(11);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      Icon = bitField[2] ? reader.ReadString() : null;
      Name = bitField[3] ? LocalizedString.ReadRecord(reader) : null;
      Description = bitField[4] ? LocalizedString.ReadRecord(reader) : null;
      Description2 = bitField[5] ? LocalizedString.ReadRecord(reader) : null;
      Label = bitField[6] ? reader.ReadString() : null;
      if (bitField[7])
        Level = reader.ReadInt32();
      if (bitField[8])
        SlotType = reader.ReadByteEnum<PerkSlotType>();
      HeroDependency = bitField[9] ? Key.ReadRecord(reader) : null;
      PerkMods = bitField[10] ? reader.ReadList<PerkMod, List<PerkMod>>(PerkMod.ReadVariant) : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardPerk value) => value.Write(writer);

    public static CardPerk ReadRecord(BinaryReader reader)
    {
      var cardPerk = new CardPerk();
      cardPerk.Read(reader);
      return cardPerk;
    }
}