using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class StatusCustomGame
{
    public ulong Id { get; set; }
    public string? Name { get; set; }
    public bool Private { get; set; }
    public int Players { get; set; }
    public int MaxPlayers { get; set; }
    public CustomGameStatus Status { get; set; }
    public string? StatusDescription { get; set; }
    public List<StatusCustomGamePlayer> PlayerList { get; set; } = [];

    public static void WriteRecord(BinaryWriter writer, StatusCustomGame value)
    {
        writer.Write(value.Id);
        writer.WriteOption(value.Name, writer.Write);
        writer.Write(value.Private);
        writer.Write(value.Players);
        writer.Write(value.MaxPlayers);
        writer.WriteByteEnum(value.Status);
        writer.WriteOption(value.StatusDescription, writer.Write);
        writer.WriteList(value.PlayerList, StatusCustomGamePlayer.WriteRecord);
    }

    public static StatusCustomGame ReadRecord(BinaryReader reader)
    {
        return new StatusCustomGame
        {
            Id = reader.ReadUInt64(),
            Name = reader.ReadOption(reader.ReadString),
            Private = reader.ReadBoolean(),
            Players = reader.ReadInt32(),
            MaxPlayers = reader.ReadInt32(),
            Status = reader.ReadByteEnum<CustomGameStatus>(),
            StatusDescription = reader.ReadOption(reader.ReadString),
            PlayerList = reader.ReadList<StatusCustomGamePlayer, List<StatusCustomGamePlayer>>(StatusCustomGamePlayer.ReadRecord)
        };
    }
}

public class StatusCustomGamePlayer
{
    public uint Id { get; set; }
    public string? Nickname { get; set; }
    public bool Owner { get; set; }
    public TeamType Team { get; set; }

    public static void WriteRecord(BinaryWriter writer, StatusCustomGamePlayer value)
    {
        writer.Write(value.Id);
        writer.WriteOption(value.Nickname, writer.Write);
        writer.Write(value.Owner);
        writer.WriteByteEnum(value.Team);
    }

    public static StatusCustomGamePlayer ReadRecord(BinaryReader reader)
    {
        return new StatusCustomGamePlayer
        {
            Id = reader.ReadUInt32(),
            Nickname = reader.ReadOption(reader.ReadString),
            Owner = reader.ReadBoolean(),
            Team = reader.ReadByteEnum<TeamType>()
        };
    }
}
