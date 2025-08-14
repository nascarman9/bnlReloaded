using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ZonePlayerInfo
{
    public string? Nickname { get; set; }

    public ulong? SteamId { get; set; }

    public ulong? SquadId { get; set; }

    public bool LookingForFriends { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(Nickname != null, SteamId.HasValue, SquadId.HasValue, true).Write(writer);
        if (Nickname != null)
            writer.Write(Nickname);
        if (SteamId.HasValue)
            writer.Write(SteamId.Value);
        if (SquadId.HasValue)
            writer.Write(SquadId.Value);
        writer.Write(LookingForFriends);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(4);
        bitField.Read(reader);
        Nickname = bitField[0] ? reader.ReadString() : null;
        SteamId = bitField[1] ? reader.ReadUInt64() : null;
        SquadId = bitField[2] ? reader.ReadUInt64() : null;
        if (!bitField[3])
            return;
        LookingForFriends = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, ZonePlayerInfo value)
    {
        value.Write(writer);
    }

    public static ZonePlayerInfo ReadRecord(BinaryReader reader)
    {
        var zonePlayerInfo = new ZonePlayerInfo();
        zonePlayerInfo.Read(reader);
        return zonePlayerInfo;
    }
}