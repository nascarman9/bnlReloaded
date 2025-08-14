using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ChallengeLogic
{
    public List<Key>? Challenges { get; set; }

    public List<ChallengeType>? Slots { get; set; }

    public int RefusesPerDay { get; set; }

    public float DailyChallengeUpdateHour { get; set; }

    public float BeatFriendRewardBonus { get; set; }

    public float ChallengeCleanupDays { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(Challenges != null, Slots != null, true, true, true, true).Write(writer);
      if (Challenges != null)
        writer.WriteList(Challenges, Key.WriteRecord);
      if (Slots != null)
        writer.WriteList(Slots, writer.WriteByteEnum);
      writer.Write(RefusesPerDay);
      writer.Write(DailyChallengeUpdateHour);
      writer.Write(BeatFriendRewardBonus);
      writer.Write(ChallengeCleanupDays);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(6);
      bitField.Read(reader);
      Challenges = bitField[0] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      Slots = bitField[1] ? reader.ReadList<ChallengeType, List<ChallengeType>>(reader.ReadByteEnum<ChallengeType>) : null;
      if (bitField[2])
        RefusesPerDay = reader.ReadInt32();
      if (bitField[3])
        DailyChallengeUpdateHour = reader.ReadSingle();
      if (bitField[4])
        BeatFriendRewardBonus = reader.ReadSingle();
      if (!bitField[5])
        return;
      ChallengeCleanupDays = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, ChallengeLogic value)
    {
      value.Write(writer);
    }

    public static ChallengeLogic ReadRecord(BinaryReader reader)
    {
      var challengeLogic = new ChallengeLogic();
      challengeLogic.Read(reader);
      return challengeLogic;
    }
}