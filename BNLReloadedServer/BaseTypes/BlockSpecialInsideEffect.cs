using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class BlockSpecialInsideEffect : BlockSpecial
{
    public override BlockSpecialType Type => BlockSpecialType.InsideEffect;

    public RelativeTeamType TriggerTeam { get; set; }

    public BlockInsideEffect? EnterEffect { get; set; }

    public BlockInsideEffect? IntervalEffect { get; set; }

    public float? Interval { get; set; }

    public List<Key>? InsideEffects { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(true, EnterEffect != null, IntervalEffect != null, Interval.HasValue, InsideEffects != null).Write(writer);
      writer.WriteByteEnum(TriggerTeam);
      if (EnterEffect != null)
        BlockInsideEffect.WriteRecord(writer, EnterEffect);
      if (IntervalEffect != null)
        BlockInsideEffect.WriteRecord(writer, IntervalEffect);
      if (Interval.HasValue)
        writer.Write(Interval.Value);
      if (InsideEffects == null)
        return;
      writer.WriteList(InsideEffects, Key.WriteRecord);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      if (bitField[0])
        TriggerTeam = reader.ReadByteEnum<RelativeTeamType>();
      EnterEffect = bitField[1] ? BlockInsideEffect.ReadRecord(reader) : null;
      IntervalEffect = bitField[2] ? BlockInsideEffect.ReadRecord(reader) : null;
      Interval = bitField[3] ? reader.ReadSingle() : null;
      InsideEffects = bitField[4] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, BlockSpecialInsideEffect value)
    {
      value.Write(writer);
    }

    public static BlockSpecialInsideEffect ReadRecord(BinaryReader reader)
    {
      var specialInsideEffect = new BlockSpecialInsideEffect();
      specialInsideEffect.Read(reader);
      return specialInsideEffect;
    }
}