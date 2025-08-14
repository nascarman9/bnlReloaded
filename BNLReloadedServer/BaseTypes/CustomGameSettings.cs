using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CustomGameSettings
{
    public MapInfo? MapInfo { get; set; }

    public float? BuildTime { get; set; }

    public float? RespawnTimeMod { get; set; }

    public bool? HeroSwitch { get; set; }

    public bool? SuperSupply { get; set; }

    public bool? AllowBackfilling { get; set; }

    public float? ResourceCap { get; set; }

    public float? InitResource { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(MapInfo != null, BuildTime.HasValue, RespawnTimeMod.HasValue, HeroSwitch.HasValue,
        SuperSupply.HasValue, AllowBackfilling.HasValue, ResourceCap.HasValue, InitResource.HasValue).Write(writer);
      if (MapInfo != null)
        MapInfo.WriteVariant(writer, MapInfo);
      if (BuildTime.HasValue)
        writer.Write(BuildTime.Value);
      if (RespawnTimeMod.HasValue)
        writer.Write(RespawnTimeMod.Value);
      if (HeroSwitch.HasValue)
        writer.Write(HeroSwitch.Value);
      if (SuperSupply.HasValue)
        writer.Write(SuperSupply.Value);
      if (AllowBackfilling.HasValue)
        writer.Write(AllowBackfilling.Value);
      if (ResourceCap.HasValue)
        writer.Write(ResourceCap.Value);
      if (!InitResource.HasValue)
        return;
      writer.Write(InitResource.Value);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(8);
      bitField.Read(reader);
      MapInfo = bitField[0] ? MapInfo.ReadVariant(reader) : null;
      BuildTime = bitField[1] ? reader.ReadSingle() : null;
      RespawnTimeMod = bitField[2] ? reader.ReadSingle() : null;
      HeroSwitch = bitField[3] ? reader.ReadBoolean() : null;
      SuperSupply = bitField[4] ? reader.ReadBoolean() : null;
      AllowBackfilling = bitField[5] ? reader.ReadBoolean() : null;
      ResourceCap = bitField[6] ? reader.ReadSingle() : null;
      InitResource = bitField[7] ? reader.ReadSingle() : null;
    }

    public static void WriteRecord(BinaryWriter writer, CustomGameSettings value)
    {
      value.Write(writer);
    }

    public static CustomGameSettings ReadRecord(BinaryReader reader)
    {
      var customGameSettings = new CustomGameSettings();
      customGameSettings.Read(reader);
      return customGameSettings;
    }
}