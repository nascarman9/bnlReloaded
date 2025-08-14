using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MatchDevicesLogic
{
    public Dictionary<Key, float>? DeviceCostModifiers { get; set; }

    public Dictionary<Key, float>? EntityMiningResourceModifiers { get; set; }

    public Dictionary<Key, float>? BuildTimeModifiers { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(DeviceCostModifiers != null, EntityMiningResourceModifiers != null, BuildTimeModifiers != null).Write(writer);
      if (DeviceCostModifiers != null)
        writer.WriteMap(DeviceCostModifiers, Key.WriteRecord, writer.Write);
      if (EntityMiningResourceModifiers != null)
        writer.WriteMap(EntityMiningResourceModifiers, Key.WriteRecord, writer.Write);
      if (BuildTimeModifiers != null)
        writer.WriteMap(BuildTimeModifiers, Key.WriteRecord, writer.Write);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      DeviceCostModifiers = bitField[0] ? reader.ReadMap<Key, float, Dictionary<Key, float>>(Key.ReadRecord, reader.ReadSingle) : null;
      EntityMiningResourceModifiers = bitField[1] ? reader.ReadMap<Key, float, Dictionary<Key, float>>(Key.ReadRecord, reader.ReadSingle) : null;
      BuildTimeModifiers = bitField[2] ? reader.ReadMap<Key, float, Dictionary<Key, float>>(Key.ReadRecord, reader.ReadSingle) : null;
    }

    public static void WriteRecord(BinaryWriter writer, MatchDevicesLogic value)
    {
      value.Write(writer);
    }

    public static MatchDevicesLogic ReadRecord(BinaryReader reader)
    {
      var matchDevicesLogic = new MatchDevicesLogic();
      matchDevicesLogic.Read(reader);
      return matchDevicesLogic;
    }
}