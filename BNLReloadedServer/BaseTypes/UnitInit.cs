using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class UnitInit
{
    public Key Key { get; set; }

    public ZoneTransform? Transform { get; set; }

    public bool Controlled { get; set; }

    public uint? OwnerId { get; set; }

    public TeamType Team { get; set; }

    public uint? PlayerId { get; set; }

    public Key? SkinKey { get; set; }

    public List<Key>? Gears { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, Transform != null, true, OwnerId.HasValue, true, PlayerId.HasValue, SkinKey.HasValue, Gears != null).Write(writer);
      Key.WriteRecord(writer, Key);
      if (Transform != null)
        ZoneTransform.WriteRecord(writer, Transform);
      writer.Write(Controlled);
      if (OwnerId.HasValue)
        writer.Write(OwnerId.Value);
      writer.WriteByteEnum(Team);
      if (PlayerId.HasValue)
        writer.Write(PlayerId.Value);
      if (SkinKey.HasValue)
        Key.WriteRecord(writer, SkinKey.Value);
      if (Gears == null)
        return;
      writer.WriteList(Gears, Key.WriteRecord);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(8);
      bitField.Read(reader);
      if (bitField[0])
        Key = Key.ReadRecord(reader);
      Transform = bitField[1] ? ZoneTransform.ReadRecord(reader) : null;
      if (bitField[2])
        Controlled = reader.ReadBoolean();
      OwnerId = bitField[3] ? reader.ReadUInt32() : null;
      if (bitField[4])
        Team = reader.ReadByteEnum<TeamType>();
      PlayerId = bitField[5] ? reader.ReadUInt32() : null;
      SkinKey = bitField[6] ? Key.ReadRecord(reader) : null;
      Gears = bitField[7] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, UnitInit value) => value.Write(writer);

    public static UnitInit ReadRecord(BinaryReader reader)
    {
      var unitInit = new UnitInit();
      unitInit.Read(reader);
      return unitInit;
    }
}