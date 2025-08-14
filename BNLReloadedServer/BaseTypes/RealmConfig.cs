using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class RealmConfig
{
    public float ExpandTime { get; set; } = 20f;

    public float FallbackTime { get; set; } = 300f;

    public int MaxFallbacks { get; set; } = 1;

    public bool AllowBackfilling { get; set; } = true;

    public int? MaxLevel { get; set; }

    public int? MinLevel { get; set; }

    public float? MaxRating { get; set; }

    public float? MinRating { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true, MaxLevel.HasValue, MinLevel.HasValue, MaxRating.HasValue, MinRating.HasValue).Write(writer);
      writer.Write(ExpandTime);
      writer.Write(FallbackTime);
      writer.Write(MaxFallbacks);
      writer.Write(AllowBackfilling);
      if (MaxLevel.HasValue)
        writer.Write(MaxLevel.Value);
      if (MinLevel.HasValue)
        writer.Write(MinLevel.Value);
      if (MaxRating.HasValue)
        writer.Write(MaxRating.Value);
      if (!MinRating.HasValue)
        return;
      writer.Write(MinRating.Value);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(8);
      bitField.Read(reader);
      if (bitField[0])
        ExpandTime = reader.ReadSingle();
      if (bitField[1])
        FallbackTime = reader.ReadSingle();
      if (bitField[2])
        MaxFallbacks = reader.ReadInt32();
      if (bitField[3])
        AllowBackfilling = reader.ReadBoolean();
      MaxLevel = bitField[4] ? reader.ReadInt32() : null;
      MinLevel = bitField[5] ? reader.ReadInt32() : null;
      MaxRating = bitField[6] ? reader.ReadSingle() : null;
      if (bitField[7])
        MinRating = reader.ReadSingle();
      else
        MinRating = null;
    }

    public static void WriteRecord(BinaryWriter writer, RealmConfig value) => value.Write(writer);

    public static RealmConfig ReadRecord(BinaryReader reader)
    {
      var realmConfig = new RealmConfig();
      realmConfig.Read(reader);
      return realmConfig;
    }
}