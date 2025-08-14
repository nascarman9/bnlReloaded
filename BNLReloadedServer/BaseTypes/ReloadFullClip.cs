using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ReloadFullClip : Reload
{
    public override ReloadType Type => ReloadType.FullClip;

    public float ReloadTime { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true).Write(writer);
        writer.Write(ReloadTime);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        if (!bitField[0])
            return;
        ReloadTime = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, ReloadFullClip value)
    {
        value.Write(writer);
    }

    public static ReloadFullClip ReadRecord(BinaryReader reader)
    {
        var reloadFullClip = new ReloadFullClip();
        reloadFullClip.Read(reader);
        return reloadFullClip;
    }
}