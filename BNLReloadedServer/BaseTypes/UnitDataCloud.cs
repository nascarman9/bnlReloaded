using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class UnitDataCloud : UnitData
{
    public override UnitType Type => UnitType.Cloud;

    public float Range { get; set; }

    public List<Key>? InsideEffects { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(true, InsideEffects != null).Write(writer);
      writer.Write(Range);
      if (InsideEffects != null)
        writer.WriteList(InsideEffects, Key.WriteRecord);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(2);
      bitField.Read(reader);
      if (bitField[0])
        Range = reader.ReadSingle();
      InsideEffects = bitField[1] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, UnitDataCloud value) => value.Write(writer);

    public static UnitDataCloud ReadRecord(BinaryReader reader)
    {
      var unitDataCloud = new UnitDataCloud();
      unitDataCloud.Read(reader);
      return unitDataCloud;
    }
}