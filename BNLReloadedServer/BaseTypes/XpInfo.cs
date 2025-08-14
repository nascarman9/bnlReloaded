using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class XpInfo
{
    public int Level { get; set; }

    public float LevelXp { get; set; }

    public float XpForNextLevel { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true, true).Write(writer);
        writer.Write(Level);
        writer.Write(LevelXp);
        writer.Write(XpForNextLevel);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        if (bitField[0])
            Level = reader.ReadInt32();
        if (bitField[1])
            LevelXp = reader.ReadSingle();
        if (!bitField[2])
            return;
        XpForNextLevel = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, XpInfo value) => value.Write(writer);

    public static XpInfo ReadRecord(BinaryReader reader)
    {
        var xpInfo = new XpInfo();
        xpInfo.Read(reader);
        return xpInfo;
    }
}