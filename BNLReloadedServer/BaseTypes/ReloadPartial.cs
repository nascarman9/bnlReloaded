using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ReloadPartial : Reload
{
    public override ReloadType Type => ReloadType.Partial;

    public float ReloadTime { get; set; }

    public float ReloadRate { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true, true).Write(writer);
        writer.Write(ReloadTime);
        writer.Write(ReloadRate);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            ReloadTime = reader.ReadSingle();
        if (!bitField[1])
            return;
        ReloadRate = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, ReloadPartial value) => value.Write(writer);

    public static ReloadPartial ReadRecord(BinaryReader reader)
    {
        var reloadPartial = new ReloadPartial();
        reloadPartial.Read(reader);
        return reloadPartial;
    }
}