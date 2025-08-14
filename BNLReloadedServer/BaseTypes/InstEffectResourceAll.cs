using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class InstEffectResourceAll : InstEffect
{
    public override InstEffectType Type => InstEffectType.ResourceAll;

    public RelativeTeamType AffectedTeam { get; set; }

    public bool IgnoreCasterPlayer { get; set; }

    public bool IncludeDeadPlayers { get; set; }

    public float Resource { get; set; }

    public bool Supply { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Interrupt != null, Targeting != null, Impact.HasValue, true, true, true, true, true).Write(writer);
      if (Interrupt != null)
        EffectInterrupt.WriteRecord(writer, Interrupt);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (Impact.HasValue)
        Key.WriteRecord(writer, Impact.Value);
      writer.WriteByteEnum(AffectedTeam);
      writer.Write(IgnoreCasterPlayer);
      writer.Write(IncludeDeadPlayers);
      writer.Write(Resource);
      writer.Write(Supply);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(8);
      bitField.Read(reader);
      Interrupt = bitField[0] ? EffectInterrupt.ReadRecord(reader) : null;
      Targeting = bitField[1] ? EffectTargeting.ReadRecord(reader) : null;
      Impact = bitField[2] ? Key.ReadRecord(reader) : null;
      if (bitField[3])
        AffectedTeam = reader.ReadByteEnum<RelativeTeamType>();
      if (bitField[4])
        IgnoreCasterPlayer = reader.ReadBoolean();
      if (bitField[5])
        IncludeDeadPlayers = reader.ReadBoolean();
      if (bitField[6])
        Resource = reader.ReadSingle();
      if (!bitField[7])
        return;
      Supply = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, InstEffectResourceAll value)
    {
      value.Write(writer);
    }

    public static InstEffectResourceAll ReadRecord(BinaryReader reader)
    {
      var effectResourceAll = new InstEffectResourceAll();
      effectResourceAll.Read(reader);
      return effectResourceAll;
    }
}