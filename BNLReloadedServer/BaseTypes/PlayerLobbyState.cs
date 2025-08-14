using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class PlayerLobbyState
{
    public uint PlayerId { get; set; }

    public ulong? SteamId { get; set; }

    public string? Nickname { get; set; }

    public ulong? SquadId { get; set; }

    public PlayerRoleType Role { get; set; }

    public int PlayerLevel { get; set; }

    public Dictionary<BadgeType, List<Key>>? SelectedBadges { get; set; }

    public TeamType Team { get; set; }

    public Key Hero { get; set; }

    public Dictionary<int, Key>? Devices { get; set; }

    public List<Key>? Perks { get; set; }

    public List<Key>? RestrictedHeroes { get; set; }

    public Key SkinKey { get; set; }

    public bool Ready { get; set; }

    public bool CanLoadout { get; set; }

    public LobbyStatus Status { get; set; }

    public bool LookingForFriends { get; set; }

    public Dictionary<Key, int>? DeviceLevels { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, SteamId.HasValue, Nickname != null, SquadId.HasValue, true, true, SelectedBadges != null, true,
        true, Devices != null, Perks != null, RestrictedHeroes != null, true, true, true, true, true,
        DeviceLevels != null).Write(writer);
      writer.Write(PlayerId);
      if (SteamId.HasValue)
        writer.Write(SteamId.Value);
      if (Nickname != null)
        writer.Write(Nickname);
      if (SquadId.HasValue)
        writer.Write(SquadId.Value);
      writer.WriteByteEnum(Role);
      writer.Write(PlayerLevel);
      if (SelectedBadges != null)
        writer.WriteMap(SelectedBadges, writer.WriteByteEnum, item => writer.WriteList(item, Key.WriteRecord));
      writer.WriteByteEnum(Team);
      Key.WriteRecord(writer, Hero);
      if (Devices != null)
        writer.WriteMap(Devices, writer.Write, Key.WriteRecord);
      if (Perks != null)
        writer.WriteList(Perks, Key.WriteRecord);
      if (RestrictedHeroes != null)
        writer.WriteList(RestrictedHeroes, Key.WriteRecord);
      Key.WriteRecord(writer, SkinKey);
      writer.Write(Ready);
      writer.Write(CanLoadout);
      writer.WriteByteEnum(Status);
      writer.Write(LookingForFriends);
      if (DeviceLevels != null)
        writer.WriteMap(DeviceLevels, Key.WriteRecord, writer.Write);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(18);
      bitField.Read(reader);
      if (bitField[0])
        PlayerId = reader.ReadUInt32();
      SteamId = bitField[1] ? reader.ReadUInt64() : null;
      Nickname = bitField[2] ? reader.ReadString() : null;
      SquadId = bitField[3] ? reader.ReadUInt64() : null;
      if (bitField[4])
        Role = reader.ReadByteEnum<PlayerRoleType>();
      if (bitField[5])
        PlayerLevel = reader.ReadInt32();
      SelectedBadges = bitField[6] ? reader.ReadMap<BadgeType, List<Key>, Dictionary<BadgeType, List<Key>>>(reader.ReadByteEnum<BadgeType>, () => reader.ReadList<Key, List<Key>>(Key.ReadRecord)) : null;
      if (bitField[7])
        Team = reader.ReadByteEnum<TeamType>();
      if (bitField[8])
        Hero = Key.ReadRecord(reader);
      Devices = bitField[9] ? reader.ReadMap<int, Key, Dictionary<int, Key>>(reader.ReadInt32, Key.ReadRecord) : null;
      Perks = bitField[10] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      RestrictedHeroes = bitField[11] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
      if (bitField[12])
        SkinKey = Key.ReadRecord(reader);
      if (bitField[13])
        Ready = reader.ReadBoolean();
      if (bitField[14])
        CanLoadout = reader.ReadBoolean();
      if (bitField[15])
        Status = reader.ReadByteEnum<LobbyStatus>();
      if (bitField[16])
        LookingForFriends = reader.ReadBoolean();
      DeviceLevels = bitField[17] ? reader.ReadMap<Key, int, Dictionary<Key, int>>(Key.ReadRecord, reader.ReadInt32) : null;
    }

    public static void WriteRecord(BinaryWriter writer, PlayerLobbyState value)
    {
      value.Write(writer);
    }

    public static PlayerLobbyState ReadRecord(BinaryReader reader)
    {
      var playerLobbyState = new PlayerLobbyState();
      playerLobbyState.Read(reader);
      return playerLobbyState;
    }
}