using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CustomGameInfo
{
    public ulong Id { get; set; }

    public string? GameName { get; set; }

    public string? StarterNickname { get; set; }

    public int Players { get; set; }

    public int MaxPlayers { get; set; }

    public bool Private { get; set; }

    public MapInfo? MapInfo { get; set; }

    public float BuildTime { get; set; }

    public float RespawnTimeMod { get; set; }

    public bool HeroSwitch { get; set; }

    public bool SuperSupply { get; set; }

    public bool AllowBackfilling { get; set; }

    public float ResourceCap { get; set; }

    public float InitResource { get; set; }

    public CustomGameStatus Status { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, GameName != null, StarterNickname != null, true, true, true, MapInfo != null, true, true, true,
        true, true, true, true, true).Write(writer);
      writer.Write(Id);
      if (GameName != null)
        writer.Write(GameName);
      if (StarterNickname != null)
        writer.Write(StarterNickname);
      writer.Write(Players);
      writer.Write(MaxPlayers);
      writer.Write(Private);
      if (MapInfo != null)
        MapInfo.WriteVariant(writer, MapInfo);
      writer.Write(BuildTime);
      writer.Write(RespawnTimeMod);
      writer.Write(HeroSwitch);
      writer.Write(SuperSupply);
      writer.Write(AllowBackfilling);
      writer.Write(ResourceCap);
      writer.Write(InitResource);
      writer.WriteByteEnum(Status);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(15);
      bitField.Read(reader);
      if (bitField[0])
        Id = reader.ReadUInt64();
      GameName = bitField[1] ? reader.ReadString() : null;
      StarterNickname = bitField[2] ? reader.ReadString() : null;
      if (bitField[3])
        Players = reader.ReadInt32();
      if (bitField[4])
        MaxPlayers = reader.ReadInt32();
      if (bitField[5])
        Private = reader.ReadBoolean();
      MapInfo = bitField[6] ? MapInfo.ReadVariant(reader) : null;
      if (bitField[7])
        BuildTime = reader.ReadSingle();
      if (bitField[8])
        RespawnTimeMod = reader.ReadSingle();
      if (bitField[9])
        HeroSwitch = reader.ReadBoolean();
      if (bitField[10])
        SuperSupply = reader.ReadBoolean();
      if (bitField[11])
        AllowBackfilling = reader.ReadBoolean();
      if (bitField[12])
        ResourceCap = reader.ReadSingle();
      if (bitField[13])
        InitResource = reader.ReadSingle();
      if (!bitField[14])
        return;
      Status = reader.ReadByteEnum<CustomGameStatus>();
    }

    public static void WriteRecord(BinaryWriter writer, CustomGameInfo value)
    {
      value.Write(writer);
    }

    public static CustomGameInfo ReadRecord(BinaryReader reader)
    {
      var customGameInfo = new CustomGameInfo();
      customGameInfo.Read(reader);
      return customGameInfo;
    }
}