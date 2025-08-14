﻿using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MatchPlayerStats
{
    public TeamType? Team { get; set; }

    public int Kills { get; set; }

    public int Deaths { get; set; }

    public int Assists { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(Team.HasValue, true, true, true).Write(writer);
        if (Team.HasValue)
            writer.WriteByteEnum(Team.Value);
        writer.Write(Kills);
        writer.Write(Deaths);
        writer.Write(Assists);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(4);
        bitField.Read(reader);
        Team = bitField[0] ? reader.ReadByteEnum<TeamType>() : null;
        if (bitField[1])
            Kills = reader.ReadInt32();
        if (bitField[2])
            Deaths = reader.ReadInt32();
        if (!bitField[3])
            return;
        Assists = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, MatchPlayerStats value)
    {
        value.Write(writer);
    }

    public static MatchPlayerStats ReadRecord(BinaryReader reader)
    {
        var matchPlayerStats = new MatchPlayerStats();
        matchPlayerStats.Read(reader);
        return matchPlayerStats;
    }
}