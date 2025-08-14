using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CardDeviceGroup : Card
{
    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.DeviceGroup;

    public LocalizedString? Name { get; set; }

    public LocalizedString? Description { get; set; }

    public bool IsUpgradable { get; set; } = true;

    public BlockCategory BlockCategory { get; set; } = BlockCategory.Device;

    public List<Key>? Devices { get; set; }

    public List<long> RubblePrices { get; set; } = [];

    public List<long> GoldPrices { get; set; } = [];

    public Key? RubbleKey { get; set; }

    public Dictionary<string, string> UpgradableStats { get; set; } = new();

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, Name != null, Description != null, true, true, Devices != null,
        true, true, RubbleKey.HasValue, true).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (Name != null)
        LocalizedString.WriteRecord(writer, Name);
      if (Description != null)
        LocalizedString.WriteRecord(writer, Description);
      writer.Write(IsUpgradable);
      writer.WriteByteEnum(BlockCategory);
      if (Devices != null)
        writer.WriteList(Devices, Key.WriteRecord);
      writer.WriteList(RubblePrices, writer.Write);
      writer.WriteList(GoldPrices, writer.Write);
      if (RubbleKey.HasValue)
        Key.WriteRecord(writer, RubbleKey.Value);
      writer.WriteMap(UpgradableStats, writer.Write, writer.Write);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(11);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      Name = bitField[2] ? LocalizedString.ReadRecord(reader) : null;
      Description = bitField[3] ? LocalizedString.ReadRecord(reader) : null;
      if (bitField[4])
        IsUpgradable = reader.ReadBoolean();
      if (bitField[5])
        BlockCategory = reader.ReadByteEnum<BlockCategory>();
      Devices = bitField[6] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      if (bitField[7])
        RubblePrices = reader.ReadList<long, List<long>>(reader.ReadInt64);
      if (bitField[8])
        GoldPrices = reader.ReadList<long, List<long>>(reader.ReadInt64);
      RubbleKey = bitField[9] ? Key.ReadRecord(reader) : null;
      if (bitField[10])
        UpgradableStats = reader.ReadMap<string, string, Dictionary<string, string>>(reader.ReadString, reader.ReadString);
    }

    public static void WriteRecord(BinaryWriter writer, CardDeviceGroup value)
    {
      value.Write(writer);
    }

    public static CardDeviceGroup ReadRecord(BinaryReader reader)
    {
      var cardDeviceGroup = new CardDeviceGroup();
      cardDeviceGroup.Read(reader);
      return cardDeviceGroup;
    }
}