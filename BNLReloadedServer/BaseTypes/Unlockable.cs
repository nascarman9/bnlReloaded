using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class Unlockable
{
    public LocalizedString? Description { get; set; }

    public UnlockableScopeType DeviceScope { get; set; }

    public List<Key>? Items { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(Description != null, true, Items != null).Write(writer);
      if (Description != null)
        LocalizedString.WriteRecord(writer, Description);
      writer.WriteByteEnum(DeviceScope);
      if (Items != null)
        writer.WriteList(Items, Key.WriteRecord);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      Description = bitField[0] ? LocalizedString.ReadRecord(reader) : null;
      if (bitField[1])
        DeviceScope = reader.ReadByteEnum<UnlockableScopeType>();
      Items = bitField[2] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, Unlockable value) => value.Write(writer);

    public static Unlockable ReadRecord(BinaryReader reader)
    {
      var unlockable = new Unlockable();
      unlockable.Read(reader);
      return unlockable;
    }
}