using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class AmmoPool
{
    public float PoolSize { get; set; }

    public float BaseRegen { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true).Write(writer);
        writer.Write(PoolSize);
        writer.Write(BaseRegen);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            PoolSize = reader.ReadSingle();
        if (!bitField[1])
            return;
        BaseRegen = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, AmmoPool value) => value.Write(writer);

    public static AmmoPool ReadRecord(BinaryReader reader)
    {
        var ammoPool = new AmmoPool();
        ammoPool.Read(reader);
        return ammoPool;
    }
}