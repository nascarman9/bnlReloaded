using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.ProtocolInterfaces;

namespace BNLReloadedServer.BaseTypes;

public class CardHeroClass : Card, IIcon
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.HeroClass;

    public string? Icon { get; set; }

    public HeroClassType Type { get; set; }

    public LocalizedString? Name { get; set; }

    public LocalizedString? Description { get; set; }

    public float HeroesHeight { get; set; }

    public List<Key>? AvailableDevices { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, Icon != null, true, Name != null, Description != null, true, AvailableDevices != null).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (Icon != null)
        writer.Write(Icon);
      writer.WriteByteEnum(Type);
      if (Name != null)
        LocalizedString.WriteRecord(writer, Name);
      if (Description != null)
        LocalizedString.WriteRecord(writer, Description);
      writer.Write(HeroesHeight);
      if (AvailableDevices != null)
        writer.WriteList(AvailableDevices, Key.WriteRecord);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(8);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      Icon = bitField[2] ? reader.ReadString() : null;
      if (bitField[3])
        Type = reader.ReadByteEnum<HeroClassType>();
      Name = bitField[4] ? LocalizedString.ReadRecord(reader) : null;
      Description = bitField[5] ? LocalizedString.ReadRecord(reader) : null;
      if (bitField[6])
        HeroesHeight = reader.ReadSingle();
      AvailableDevices = bitField[7] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardHeroClass value) => value.Write(writer);

    public static CardHeroClass ReadRecord(BinaryReader reader)
    {
      var cardHeroClass = new CardHeroClass();
      cardHeroClass.Read(reader);
      return cardHeroClass;
    }
}