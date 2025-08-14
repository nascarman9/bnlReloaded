using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class InstEffectAllPlayersPersistent : InstEffect
{
    public override InstEffectType Type => InstEffectType.AllPlayersPersistent;

    public RelativeTeamType AffectedTeam { get; set; }

    public bool IncludeDeadPlayers { get; set; }

    public float PersistenceDuration { get; set; }

    public List<Key>? Constant { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Interrupt != null, Targeting != null, Impact.HasValue, true, true, true, Constant != null).Write(writer);
      if (Interrupt != null)
        EffectInterrupt.WriteRecord(writer, Interrupt);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      if (Impact.HasValue)
        Key.WriteRecord(writer, Impact.Value);
      writer.WriteByteEnum(AffectedTeam);
      writer.Write(IncludeDeadPlayers);
      writer.Write(PersistenceDuration);
      if (Constant != null)
        writer.WriteList(Constant, Key.WriteRecord);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(7);
      bitField.Read(reader);
      Interrupt = bitField[0] ? EffectInterrupt.ReadRecord(reader) : null;
      Targeting = bitField[1] ? EffectTargeting.ReadRecord(reader) : null;
      Impact = bitField[2] ? Key.ReadRecord(reader) : null;
      if (bitField[3])
        AffectedTeam = reader.ReadByteEnum<RelativeTeamType>();
      if (bitField[4])
        IncludeDeadPlayers = reader.ReadBoolean();
      if (bitField[5])
        PersistenceDuration = reader.ReadSingle();
      Constant = bitField[6] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, InstEffectAllPlayersPersistent value)
    {
      value.Write(writer);
    }

    public static InstEffectAllPlayersPersistent ReadRecord(BinaryReader reader)
    {
      var playersPersistent = new InstEffectAllPlayersPersistent();
      playersPersistent.Read(reader);
      return playersPersistent;
    }
}