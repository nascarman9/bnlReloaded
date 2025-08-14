using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ChallengeFriendInfo
{
    public uint Id { get; set; }

    public string? Name { get; set; }

    public ChallengeResult? Result { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, Name != null, Result != null).Write(writer);
        writer.Write(Id);
        if (Name != null)
            writer.Write(Name);
        if (Result != null)
            ChallengeResult.WriteRecord(writer, Result);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        if (bitField[0])
            Id = reader.ReadUInt32();
        Name = bitField[1] ? reader.ReadString() : null;
        Result = bitField[2] ? ChallengeResult.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, ChallengeFriendInfo value)
    {
        value.Write(writer);
    }

    public static ChallengeFriendInfo ReadRecord(BinaryReader reader)
    {
        var challengeFriendInfo = new ChallengeFriendInfo();
        challengeFriendInfo.Read(reader);
        return challengeFriendInfo;
    }
}