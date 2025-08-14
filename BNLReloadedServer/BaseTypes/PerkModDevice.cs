using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class PerkModDevice : PerkMod
{
    public override PerkModType Type => PerkModType.Device;

    public List<Key>? ReplaceFrom { get; set; }

    public Key ReplaceTo { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(ReplaceFrom != null, true).Write(writer);
      if (ReplaceFrom != null)
        writer.WriteList(ReplaceFrom, Key.WriteRecord);
      Key.WriteRecord(writer, ReplaceTo);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(2);
      bitField.Read(reader);
      ReplaceFrom = bitField[0] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      if (!bitField[1])
        return;
      ReplaceTo = Key.ReadRecord(reader);
    }

    public static void WriteRecord(BinaryWriter writer, PerkModDevice value) => value.Write(writer);

    public static PerkModDevice ReadRecord(BinaryReader reader)
    {
      var perkModDevice = new PerkModDevice();
      perkModDevice.Read(reader);
      return perkModDevice;
    }
}