using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class CustomGameLogic
{
    public Key GameMode { get; set; }

    public int MaxSpectatorsPerMatch { get; set; }

    public float BackfillerWarningTime { get; set; }

    public float BackfillerKickTime { get; set; }

    public float MinBuildTime { get; set; }

    public float MaxBuildTime { get; set; }

    public float DefaultBuildTime { get; set; }

    public float MinRespawnTimeMod { get; set; }

    public float MaxRespawnTimeMod { get; set; }

    public float DefaultResourceCap { get; set; }

    public float MinResourceCap { get; set; }

    public float MaxResourceCap { get; set; }

    public float DefaultInitResource { get; set; }

    public float MinInitResource { get; set; }

    public float MaxInitResource { get; set; }

    public bool StopOnCreatorLeave { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true).Write(writer);
      Key.WriteRecord(writer, GameMode);
      writer.Write(MaxSpectatorsPerMatch);
      writer.Write(BackfillerWarningTime);
      writer.Write(BackfillerKickTime);
      writer.Write(MinBuildTime);
      writer.Write(MaxBuildTime);
      writer.Write(DefaultBuildTime);
      writer.Write(MinRespawnTimeMod);
      writer.Write(MaxRespawnTimeMod);
      writer.Write(DefaultResourceCap);
      writer.Write(MinResourceCap);
      writer.Write(MaxResourceCap);
      writer.Write(DefaultInitResource);
      writer.Write(MinInitResource);
      writer.Write(MaxInitResource);
      writer.Write(StopOnCreatorLeave);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(16);
      bitField.Read(reader);
      if (bitField[0])
        GameMode = Key.ReadRecord(reader);
      if (bitField[1])
        MaxSpectatorsPerMatch = reader.ReadInt32();
      if (bitField[2])
        BackfillerWarningTime = reader.ReadSingle();
      if (bitField[3])
        BackfillerKickTime = reader.ReadSingle();
      if (bitField[4])
        MinBuildTime = reader.ReadSingle();
      if (bitField[5])
        MaxBuildTime = reader.ReadSingle();
      if (bitField[6])
        DefaultBuildTime = reader.ReadSingle();
      if (bitField[7])
        MinRespawnTimeMod = reader.ReadSingle();
      if (bitField[8])
        MaxRespawnTimeMod = reader.ReadSingle();
      if (bitField[9])
        DefaultResourceCap = reader.ReadSingle();
      if (bitField[10])
        MinResourceCap = reader.ReadSingle();
      if (bitField[11])
        MaxResourceCap = reader.ReadSingle();
      if (bitField[12])
        DefaultInitResource = reader.ReadSingle();
      if (bitField[13])
        MinInitResource = reader.ReadSingle();
      if (bitField[14])
        MaxInitResource = reader.ReadSingle();
      if (!bitField[15])
        return;
      StopOnCreatorLeave = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, CustomGameLogic value)
    {
      value.Write(writer);
    }

    public static CustomGameLogic ReadRecord(BinaryReader reader)
    {
      var customGameLogic = new CustomGameLogic();
      customGameLogic.Read(reader);
      return customGameLogic;
    }
}