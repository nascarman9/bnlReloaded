using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class PerksLogic
{
    public List<Key>? Offensive { get; set; }

    public List<Key>? Defensive { get; set; }

    public Dictionary<Key, List<Key>>? Heroes { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(Offensive != null, Defensive != null, Heroes != null).Write(writer);
      if (Offensive != null)
        writer.WriteList(Offensive, Key.WriteRecord);
      if (Defensive != null)
        writer.WriteList(Defensive, Key.WriteRecord);
      if (Heroes != null)
        writer.WriteMap(Heroes, Key.WriteRecord, item => writer.WriteList(item, Key.WriteRecord));
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      Offensive = bitField[0] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      Defensive = bitField[1] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      Heroes = bitField[2] ? reader.ReadMap<Key, List<Key>, Dictionary<Key, List<Key>>>(Key.ReadRecord, () => reader.ReadList<Key, List<Key>>(Key.ReadRecord)) : null;
    }

    public static void WriteRecord(BinaryWriter writer, PerksLogic value) => value.Write(writer);

    public static PerksLogic ReadRecord(BinaryReader reader)
    {
      var perksLogic = new PerksLogic();
      perksLogic.Read(reader);
      return perksLogic;
    }
}