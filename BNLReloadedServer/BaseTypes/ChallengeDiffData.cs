using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ChallengeDiffData
{
    public Key Key { get; set; }

    public bool Completed { get; set; }

    public ChallengeResult? OldResult { get; set; }

    public ChallengeResult? NewResult { get; set; }

    public ChallengeFriendInfo? FriendInfo { get; set; }

    public bool? BetterThanFriend { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, OldResult != null, NewResult != null, FriendInfo != null, BetterThanFriend.HasValue).Write(writer);
      Key.WriteRecord(writer, Key);
      writer.Write(Completed);
      if (OldResult != null)
        ChallengeResult.WriteRecord(writer, OldResult);
      if (NewResult != null)
        ChallengeResult.WriteRecord(writer, NewResult);
      if (FriendInfo != null)
        ChallengeFriendInfo.WriteRecord(writer, FriendInfo);
      if (!BetterThanFriend.HasValue)
        return;
      writer.Write(BetterThanFriend.Value);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(6);
      bitField.Read(reader);
      if (bitField[0])
        Key = Key.ReadRecord(reader);
      if (bitField[1])
        Completed = reader.ReadBoolean();
      OldResult = bitField[2] ? ChallengeResult.ReadRecord(reader) : null;
      NewResult = bitField[3] ? ChallengeResult.ReadRecord(reader) : null;
      FriendInfo = bitField[4] ? ChallengeFriendInfo.ReadRecord(reader) : null;
      BetterThanFriend = bitField[5] ? reader.ReadBoolean() : null;
    }

    public static void WriteRecord(BinaryWriter writer, ChallengeDiffData value)
    {
      value.Write(writer);
    }

    public static ChallengeDiffData ReadRecord(BinaryReader reader)
    {
      var challengeDiffData = new ChallengeDiffData();
      challengeDiffData.Read(reader);
      return challengeDiffData;
    }
}