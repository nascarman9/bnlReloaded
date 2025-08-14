using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class BlockSpecialSlippery : BlockSpecial
{
    public override BlockSpecialType Type => BlockSpecialType.Slippery;

    public float AccelerationMod { get; set; }

    public float FrictionMod { get; set; }

    public RelativeTeamType AffectTeam { get; set; }

    public override void Write(BinaryWriter writer)
    {
      new BitField(true, true, true).Write(writer); 
      writer.Write(AccelerationMod);
      writer.Write(FrictionMod);
      writer.WriteByteEnum(AffectTeam);
    }

    public override void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      if (bitField[0])
        AccelerationMod = reader.ReadSingle();
      if (bitField[1])
        FrictionMod = reader.ReadSingle();
      if (!bitField[2])
        return;
      AffectTeam = reader.ReadByteEnum<RelativeTeamType>();
    }

    public static void WriteRecord(BinaryWriter writer, BlockSpecialSlippery value)
    {
      value.Write(writer);
    }

    public static BlockSpecialSlippery ReadRecord(BinaryReader reader)
    {
      var blockSpecialSlippery = new BlockSpecialSlippery();
      blockSpecialSlippery.Read(reader);
      return blockSpecialSlippery;
    }
}