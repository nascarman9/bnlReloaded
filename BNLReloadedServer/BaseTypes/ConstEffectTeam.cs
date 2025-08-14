using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ConstEffectTeam : ConstEffect
{
    public override ConstEffectType Type => ConstEffectType.Team;

    public RelativeTeamType Team { get; set; }

    public List<Key>? ConstantEffects { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(Targeting != null, true, ConstantEffects != null).Write(writer);
      if (Targeting != null)
        EffectTargeting.WriteRecord(writer, Targeting);
      writer.WriteByteEnum(Team);
      if (ConstantEffects != null)
        writer.WriteList(ConstantEffects, Key.WriteRecord);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      Targeting = bitField[0] ? EffectTargeting.ReadRecord(reader) : null;
      if (bitField[1])
        Team = reader.ReadByteEnum<RelativeTeamType>();
      ConstantEffects = bitField[2] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, ConstEffectTeam value)
    {
      value.Write(writer);
    }

    public static ConstEffectTeam ReadRecord(BinaryReader reader)
    {
      var constEffectTeam = new ConstEffectTeam();
      constEffectTeam.Read(reader);
      return constEffectTeam;
    }
}