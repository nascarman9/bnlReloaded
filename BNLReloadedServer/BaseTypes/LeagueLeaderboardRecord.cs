using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LeagueLeaderboardRecord
{
    public uint PlayerId { get; set; }

    public ulong? SteamId { get; set; }

    public string? PlayerName { get; set; }

    public int Points { get; set; }

    public int Status { get; set; }

    public int Wins { get; set; }

    public int TotalMatches { get; set; }

    public DateTime RegistrationTime { get; set; }

    public string? Region { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, SteamId.HasValue, PlayerName != null, true, true, true, true, true, Region != null).Write(writer);
      writer.Write(PlayerId);
      if (SteamId.HasValue)
        writer.Write(SteamId.Value);
      if (PlayerName != null)
        writer.Write(PlayerName);
      writer.Write(Points);
      writer.Write(Status);
      writer.Write(Wins);
      writer.Write(TotalMatches);
      writer.WriteDateTime(RegistrationTime);
      if (Region != null)
        writer.Write(Region);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(9);
      bitField.Read(reader);
      if (bitField[0])
        PlayerId = reader.ReadUInt32();
      SteamId = bitField[1] ? reader.ReadUInt64() : null;
      PlayerName = bitField[2] ? reader.ReadString() : null;
      if (bitField[3])
        Points = reader.ReadInt32();
      if (bitField[4])
        Status = reader.ReadInt32();
      if (bitField[5])
        Wins = reader.ReadInt32();
      if (bitField[6])
        TotalMatches = reader.ReadInt32();
      if (bitField[7])
        RegistrationTime = reader.ReadDateTime();
      Region = bitField[8] ? reader.ReadString() : null;
    }

    public static void WriteRecord(BinaryWriter writer, LeagueLeaderboardRecord value)
    {
      value.Write(writer);
    }

    public static LeagueLeaderboardRecord ReadRecord(BinaryReader reader)
    {
      var leaderboardRecord = new LeagueLeaderboardRecord();
      leaderboardRecord.Read(reader);
      return leaderboardRecord;
    }
}