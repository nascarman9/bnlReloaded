using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;
using BNLReloadedServer.ProtocolInterfaces;

namespace BNLReloadedServer.BaseTypes;

public class CardDevice : Card, IIcon, IUnlockable, ILootCrateItem
{
    public Key? DeviceKeyAtLevel(byte level) => DeviceLevels?[level - 1];
    
    [JsonIgnore]
    public Key StartingDeviceKey => DeviceKeyAtLevel(1)!.Value;

    [JsonPropertyOrder(-3)]
    public override CardCategory Category => CardCategory.Device;

    public string? Icon { get; set; }

    public Key GroupKey { get; set; }

    public LocalizedString? Name { get; set; }

    public LocalizedString? Description { get; set; }

    public string? LearningVideo { get; set; }

    public bool FullBlock { get; set; }

    public bool AttachFloor { get; set; } = true;

    public bool AttachCeiling { get; set; } = true;

    public bool AttachWalls { get; set; } = true;

    public bool InverseDirection { get; set; }

    public float? GhostRadius { get; set; }

    public float? GhostSector { get; set; }

    public List<Key>? DeviceLevels { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Id != null, true, Icon != null, true, Name != null, Description != null, LearningVideo != null, true,
        true, true, true, true, GhostRadius.HasValue, GhostSector.HasValue, DeviceLevels != null).Write(writer);
      if (Id != null)
        writer.Write(Id);
      writer.WriteByteEnum(Scope);
      if (Icon != null)
        writer.Write(Icon);
      Key.WriteRecord(writer, GroupKey);
      if (Name != null)
        LocalizedString.WriteRecord(writer, Name);
      if (Description != null)
        LocalizedString.WriteRecord(writer, Description);
      if (LearningVideo != null)
        writer.Write(LearningVideo);
      writer.Write(FullBlock);
      writer.Write(AttachFloor);
      writer.Write(AttachCeiling);
      writer.Write(AttachWalls);
      writer.Write(InverseDirection);
      if (GhostRadius.HasValue)
        writer.Write(GhostRadius.Value);
      if (GhostSector.HasValue)
        writer.Write(GhostSector.Value);
      if (DeviceLevels != null)
        writer.WriteList(DeviceLevels, Key.WriteRecord);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(15);
      bitField.Read(reader);
      Id = bitField[0] ? reader.ReadString() : null;
      if (bitField[1])
        Scope = reader.ReadByteEnum<ScopeType>();
      Icon = bitField[2] ? reader.ReadString() : null;
      if (bitField[3])
        GroupKey = Key.ReadRecord(reader);
      Name = bitField[4] ? LocalizedString.ReadRecord(reader) : null;
      Description = bitField[5] ? LocalizedString.ReadRecord(reader) : null;
      LearningVideo = bitField[6] ? reader.ReadString() : null;
      if (bitField[7])
        FullBlock = reader.ReadBoolean();
      if (bitField[8])
        AttachFloor = reader.ReadBoolean();
      if (bitField[9])
        AttachCeiling = reader.ReadBoolean();
      if (bitField[10])
        AttachWalls = reader.ReadBoolean();
      if (bitField[11])
        InverseDirection = reader.ReadBoolean();
      GhostRadius = bitField[12] ? reader.ReadSingle() : null;
      GhostSector = bitField[13] ? reader.ReadSingle() : null;
      DeviceLevels = bitField[14] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, CardDevice value) => value.Write(writer);

    public static CardDevice ReadRecord(BinaryReader reader)
    {
      var cardDevice = new CardDevice();
      cardDevice.Read(reader);
      return cardDevice;
    }
}