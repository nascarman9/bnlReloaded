using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class Ammo
{
    public int Index { get; set; }

    public float? Mag { get; set; }

    public float? Pool { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, Mag.HasValue, Pool.HasValue).Write(writer);
        writer.Write(Index);
        if (Mag.HasValue)
            writer.Write(Mag.Value);
        if (!Pool.HasValue)
            return;
        writer.Write(Pool.Value);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        if (bitField[0])
            Index = reader.ReadInt32();
        Mag = bitField[1] ? reader.ReadSingle() : null;
        Pool = bitField[2] ? reader.ReadSingle() : null;
    }

    public static void WriteRecord(BinaryWriter writer, Ammo value) => value.Write(writer);

    public static Ammo ReadRecord(BinaryReader reader)
    {
        var ammo = new Ammo();
        ammo.Read(reader);
        return ammo;
    }
}