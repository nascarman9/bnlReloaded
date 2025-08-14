using System.Text.Json.Serialization;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MapDataProps
{
    public void ResetBarriers(float sizeX)
    {
      var num1 = (float) (5.0 * sizeX / 12.0);
      Barrier2Team1 = num1;
      Barrier1Team1 = num1;
      var num2 = (float) (7.0 * sizeX / 12.0);
      Barrier2Team2 = num2;
      Barrier1Team2 = num2;
      FixBarriers();
    }

    public void FixBarriers()
    {
      Barrier1Team1 = FixBarierPos(Barrier1Team1);
      Barrier1Team2 = FixBarierPos(Barrier1Team2);
      Barrier2Team1 = FixBarierPos(Barrier2Team1);
      Barrier2Team2 = FixBarierPos(Barrier2Team2);
    }

    public static float FixBarierPos(float value) => (float) Math.Floor(value) + 0.5f;

    public string AudioAmbience { get; set; } = string.Empty;

    public string Render { get; set; } = string.Empty;

    public string Plane { get; set; } = string.Empty;

    public float PlanePosition { get; set; } = -1f;

    public float KillPosition { get; set; } = -1f;

    [JsonPropertyName("barrier_1_team_1")]
    public float Barrier1Team1 { get; set; }

    [JsonPropertyName("barrier_1_team_2")]
    public float Barrier1Team2 { get; set; }

    [JsonPropertyName("barrier_2_team_1")]
    public float Barrier2Team1 { get; set; }

    [JsonPropertyName("barrier_2_team_2")]
    public float Barrier2Team2 { get; set; }

    public float MinFallHeight { get; set; } = 5f;

    public float MaxFallHeight { get; set; } = 25f;

    public float? BuildTime { get; set; }

    public float? StartingResources { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true, true, true, true, true, true, true, true, BuildTime.HasValue, StartingResources.HasValue).Write(writer);
      writer.Write(AudioAmbience);
      writer.Write(Render);
      writer.Write(Plane);
      writer.Write(PlanePosition);
      writer.Write(KillPosition);
      writer.Write(Barrier1Team1);
      writer.Write(Barrier1Team2);
      writer.Write(Barrier2Team1);
      writer.Write(Barrier2Team2);
      writer.Write(MinFallHeight);
      writer.Write(MaxFallHeight);
      if (BuildTime.HasValue)
        writer.Write(BuildTime.Value);
      if (!StartingResources.HasValue)
        return;
      writer.Write(StartingResources.Value);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(13);
      bitField.Read(reader);
      if (bitField[0])
        AudioAmbience = reader.ReadString();
      if (bitField[1])
        Render = reader.ReadString();
      if (bitField[2])
        Plane = reader.ReadString();
      if (bitField[3])
        PlanePosition = reader.ReadSingle();
      if (bitField[4])
        KillPosition = reader.ReadSingle();
      if (bitField[5])
        Barrier1Team1 = reader.ReadSingle();
      if (bitField[6])
        Barrier1Team2 = reader.ReadSingle();
      if (bitField[7])
        Barrier2Team1 = reader.ReadSingle();
      if (bitField[8])
        Barrier2Team2 = reader.ReadSingle();
      if (bitField[9])
        MinFallHeight = reader.ReadSingle();
      if (bitField[10])
        MaxFallHeight = reader.ReadSingle();
      BuildTime = bitField[11] ? reader.ReadSingle() : null;
      StartingResources = bitField[12] ? reader.ReadSingle() : null;
    }

    public static void WriteRecord(BinaryWriter writer, MapDataProps value) => value.Write(writer);

    public static MapDataProps ReadRecord(BinaryReader reader)
    {
      var mapDataProps = new MapDataProps();
      mapDataProps.Read(reader);
      return mapDataProps;
    }
}