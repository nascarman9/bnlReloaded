using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ReticleInfo
{
    public ReticleType Type { get; set; }

    public ReticuleApplicationType Application { get; set; }

    public float PreviewDamageRange { get; set; }

    public float PreviewMaxRange { get; set; }

    public bool ShowInAds { get; set; } = true;

    public float? MeleeHeelingAngle { get; set; }

    public float? MeleeArcAngle { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true, true, MeleeHeelingAngle.HasValue, MeleeArcAngle.HasValue).Write(writer);
      writer.WriteByteEnum(Type);
      writer.WriteByteEnum(Application);
      writer.Write(PreviewDamageRange);
      writer.Write(PreviewMaxRange);
      writer.Write(ShowInAds);
      if (MeleeHeelingAngle.HasValue)
        writer.Write(MeleeHeelingAngle.Value);
      if (!MeleeArcAngle.HasValue)
        return;
      writer.Write(MeleeArcAngle.Value);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(7);
      bitField.Read(reader);
      if (bitField[0])
        Type = reader.ReadByteEnum<ReticleType>();
      if (bitField[1])
        Application = reader.ReadByteEnum<ReticuleApplicationType>();
      if (bitField[2])
        PreviewDamageRange = reader.ReadSingle();
      if (bitField[3])
        PreviewMaxRange = reader.ReadSingle();
      if (bitField[4])
        ShowInAds = reader.ReadBoolean();
      MeleeHeelingAngle = bitField[5] ? reader.ReadSingle() : null;
      MeleeArcAngle = bitField[6] ? reader.ReadSingle() : null;
    }

    public static void WriteRecord(BinaryWriter writer, ReticleInfo value) => value.Write(writer);

    public static ReticleInfo ReadRecord(BinaryReader reader)
    {
      var reticleInfo = new ReticleInfo();
      reticleInfo.Read(reader);
      return reticleInfo;
    }
}