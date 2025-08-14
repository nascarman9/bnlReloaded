using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CustomGamePlayer
{
    public uint Id { get; set; }

    public ulong? SteamId { get; set; }

    public string? Nickname { get; set; }

    public int PlayerLevel { get; set; }

    public Dictionary<BadgeType, List<Key>>? SelectedBadges { get; set; }

    public bool Owner { get; set; }

    public TeamType Team { get; set; }

    public bool SwitchTeamRequest { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, SteamId.HasValue, Nickname != null, true, SelectedBadges != null, true, true, true).Write(writer);
      writer.Write(Id);
      if (SteamId.HasValue)
        writer.Write(SteamId.Value);
      if (Nickname != null)
        writer.Write(Nickname);
      writer.Write(PlayerLevel);
      if (SelectedBadges != null)
        writer.WriteMap(SelectedBadges, writer.WriteByteEnum, item => writer.WriteList(item, Key.WriteRecord));
      writer.Write(Owner);
      writer.WriteByteEnum(Team);
      writer.Write(SwitchTeamRequest);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(8);
      bitField.Read(reader);
      if (bitField[0])
        Id = reader.ReadUInt32();
      SteamId = bitField[1] ? reader.ReadUInt64() : null;
      Nickname = bitField[2] ? reader.ReadString() : null;
      if (bitField[3])
        PlayerLevel = reader.ReadInt32();
      SelectedBadges = bitField[4] ? reader.ReadMap<BadgeType, List<Key>, Dictionary<BadgeType, List<Key>>>(reader.ReadByteEnum<BadgeType>, () => reader.ReadList<Key, List<Key>>(Key.ReadRecord)) : null;
      if (bitField[5])
        Owner = reader.ReadBoolean();
      if (bitField[6])
        Team = reader.ReadByteEnum<TeamType>();
      if (!bitField[7])
        return;
      SwitchTeamRequest = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, CustomGamePlayer value)
    {
      value.Write(writer);
    }

    public static CustomGamePlayer ReadRecord(BinaryReader reader)
    {
      var customGamePlayer = new CustomGamePlayer();
      customGamePlayer.Read(reader);
      return customGamePlayer;
    }
}