using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ProfileData
{
    public string? Nickname { get; set; }

    public ulong? SteamId { get; set; }

    public League? League { get; set; }

    public PlayerProgression? Progression { get; set; }

    public List<MatchHistoryRecord> MatchHistory { get; set; } = [];

    public List<HeroStats> HeroStats { get; set; } = [];

    public GlobalStats? GlobalStats { get; set; }

    public Dictionary<BadgeType, List<Key>>? SelectedBadges { get; set; }

    public bool LookingForFriends { get; set; }

    public int FriendsCount { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(Nickname != null, SteamId.HasValue, League != null, Progression != null, true,
        true, GlobalStats != null, SelectedBadges != null, true, true).Write(writer);
      if (Nickname != null)
        writer.Write(Nickname);
      if (SteamId.HasValue)
        writer.Write(SteamId.Value);
      if (League != null)
        League.WriteRecord(writer, League);
      if (Progression != null)
        PlayerProgression.WriteRecord(writer, Progression);
      writer.WriteList(MatchHistory, MatchHistoryRecord.WriteRecord);
      writer.WriteList(HeroStats, BaseTypes.HeroStats.WriteRecord);
      if (GlobalStats != null)
        GlobalStats.WriteRecord(writer, GlobalStats);
      if (SelectedBadges != null)
        writer.WriteMap(SelectedBadges, writer.WriteByteEnum, item => writer.WriteList(item, Key.WriteRecord));
      writer.Write(LookingForFriends);
      writer.Write(FriendsCount);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(10);
      bitField.Read(reader);
      Nickname = bitField[0] ? reader.ReadString() : null;
      SteamId = bitField[1] ? reader.ReadUInt64() : null;
      League = bitField[2] ? League.ReadRecord(reader) : null;
      Progression = bitField[3] ? PlayerProgression.ReadRecord(reader) : null;
      if (bitField[4])
        MatchHistory = reader.ReadList<MatchHistoryRecord, List<MatchHistoryRecord>>(MatchHistoryRecord.ReadRecord);
      if (bitField[5])
        HeroStats = reader.ReadList<HeroStats, List<HeroStats>>(BaseTypes.HeroStats.ReadRecord);
      GlobalStats = bitField[6] ? GlobalStats.ReadRecord(reader) : null;
      SelectedBadges = bitField[7] ? reader.ReadMap<BadgeType, List<Key>, Dictionary<BadgeType, List<Key>>>(reader.ReadByteEnum<BadgeType>, () => reader.ReadList<Key, List<Key>>(Key.ReadRecord)) : null;
      if (bitField[8])
        LookingForFriends = reader.ReadBoolean();
      if (!bitField[9])
        return;
      FriendsCount = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, ProfileData value) => value.Write(writer);

    public static ProfileData ReadRecord(BinaryReader reader)
    {
      var profileData = new ProfileData();
      profileData.Read(reader);
      return profileData;
    }
}