using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class SquadPlayerUpdate
{
    public uint PlayerId { get; set; }

    public bool IsLeader { get; set; }

    public ulong? SteamId { get; set; }

    public string? Nickname { get; set; }

    public int PlayerLevel { get; set; }

    public List<int>? HeroesLevels { get; set; }

    public Dictionary<BadgeType, List<Key>>? SelectedBadges { get; set; }

    public bool Graveyard { get; set; }

    public ulong MmBanEnd { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, SteamId.HasValue, Nickname != null, true, HeroesLevels != null, SelectedBadges != null, true, true).Write(writer);
      writer.Write(PlayerId);
      writer.Write(IsLeader);
      if (SteamId.HasValue)
        writer.Write(SteamId.Value);
      if (Nickname != null)
        writer.Write(Nickname);
      writer.Write(PlayerLevel);
      if (HeroesLevels != null)
        writer.WriteList(HeroesLevels, writer.Write);
      if (SelectedBadges != null)
        writer.WriteMap(SelectedBadges, writer.WriteByteEnum, item => writer.WriteList(item, Key.WriteRecord));
      writer.Write(Graveyard);
      writer.Write(MmBanEnd);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(9);
      bitField.Read(reader);
      if (bitField[0])
        PlayerId = reader.ReadUInt32();
      if (bitField[1])
        IsLeader = reader.ReadBoolean();
      SteamId = bitField[2] ? reader.ReadUInt64() : null;
      Nickname = bitField[3] ? reader.ReadString() : null;
      if (bitField[4])
        PlayerLevel = reader.ReadInt32();
      HeroesLevels = bitField[5] ? reader.ReadList<int, List<int>>(reader.ReadInt32) : null;
      SelectedBadges = bitField[6] ? reader.ReadMap<BadgeType, List<Key>, Dictionary<BadgeType, List<Key>>>(reader.ReadByteEnum<BadgeType>, () => reader.ReadList<Key, List<Key>>(Key.ReadRecord)) : null;
      if (bitField[7])
        Graveyard = reader.ReadBoolean();
      if (!bitField[8])
        return;
      MmBanEnd = reader.ReadUInt64();
    }

    public static void WriteRecord(BinaryWriter writer, SquadPlayerUpdate value)
    {
      value.Write(writer);
    }

    public static SquadPlayerUpdate ReadRecord(BinaryReader reader)
    {
      var squadPlayerUpdate = new SquadPlayerUpdate();
      squadPlayerUpdate.Read(reader);
      return squadPlayerUpdate;
    }
}