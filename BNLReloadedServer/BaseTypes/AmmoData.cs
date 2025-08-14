using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class AmmoData
{
    public AmmoPool? Pool { get; set; }

    public float? MagSize { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(Pool != null, MagSize.HasValue).Write(writer);
        if (Pool != null)
            AmmoPool.WriteRecord(writer, Pool);
        if (!MagSize.HasValue)
            return;
        writer.Write(MagSize.Value);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        Pool = !bitField[0] ? null : AmmoPool.ReadRecord(reader);
        if (bitField[1])
            MagSize = reader.ReadSingle();
        else
            MagSize = null;
    }

    public static void WriteRecord(BinaryWriter writer, AmmoData value) => value.Write(writer);

    public static AmmoData ReadRecord(BinaryReader reader)
    {
        var ammoData = new AmmoData();
        ammoData.Read(reader);
        return ammoData;
    }
}