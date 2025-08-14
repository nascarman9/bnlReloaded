using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class Challenge
{
    public ulong Id { get; set; }

    public Key Key { get; set; }

    public ChallengeType ChallengeType { get; set; }

    public ChallengeResult? Result { get; set; }

    public ChallengeFriendInfo? FriendInfo { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true, true, Result != null, FriendInfo != null).Write(writer);
        writer.Write(Id);
        Key.WriteRecord(writer, Key);
        writer.WriteByteEnum(ChallengeType);
        if (Result != null)
            ChallengeResult.WriteRecord(writer, Result);
        if (FriendInfo == null)
            return;
        ChallengeFriendInfo.WriteRecord(writer, FriendInfo);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(5);
        bitField.Read(reader);
        if (bitField[0])
            Id = reader.ReadUInt64();
        if (bitField[1])
            Key = Key.ReadRecord(reader);
        if (bitField[2])
            ChallengeType = reader.ReadByteEnum<ChallengeType>();
        Result = bitField[3] ? ChallengeResult.ReadRecord(reader) : null;
        FriendInfo = bitField[4] ? ChallengeFriendInfo.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, Challenge value) => value.Write(writer);

    public static Challenge ReadRecord(BinaryReader reader)
    {
        var challenge = new Challenge();
        challenge.Read(reader);
        return challenge;
    }
}