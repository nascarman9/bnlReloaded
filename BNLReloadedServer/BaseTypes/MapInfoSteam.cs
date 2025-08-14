using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MapInfoSteam : MapInfo
{
    public override MapInfoType Type => MapInfoType.Steam;

    public string? Name { get; set; }

    public string? Description { get; set; }

    public string Image { get; set; } = string.Empty;

    public ulong? SteamId { get; set; }

    public ulong? SteamFileId { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Name != null, Description != null, true, SteamId.HasValue, SteamFileId.HasValue).Write(writer);
      if (Name != null)
        writer.Write(Name);
      if (Description != null)
        writer.Write(Description);
      writer.Write(Image);
      if (SteamId.HasValue)
        writer.Write(SteamId.Value);
      if (!SteamFileId.HasValue)
        return;
      writer.Write(SteamFileId.Value);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      Name = bitField[0] ? reader.ReadString() : null;
      Description = bitField[1] ? reader.ReadString() : null;
      if (bitField[2])
        Image = reader.ReadString();
      SteamId = bitField[3] ? reader.ReadUInt64() : null;
      SteamFileId = bitField[4] ? reader.ReadUInt64() : null;
    }

    public static void WriteRecord(BinaryWriter writer, MapInfoSteam value) => value.Write(writer);

    public static MapInfoSteam ReadRecord(BinaryReader reader)
    {
      var mapInfoSteam = new MapInfoSteam();
      mapInfoSteam.Read(reader);
      return mapInfoSteam;
    }
}