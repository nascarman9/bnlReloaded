using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class KillPlainLogic
{
    public float? DistanceFromMapBorders { get; set; }

    public float Damage { get; set; }

    public float DamageInterval { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(DistanceFromMapBorders.HasValue, true, true).Write(writer);
      if (DistanceFromMapBorders.HasValue)
        writer.Write(DistanceFromMapBorders.Value);
      writer.Write(Damage);
      writer.Write(DamageInterval);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      DistanceFromMapBorders = bitField[0] ? reader.ReadSingle() : null;
      if (bitField[1])
        Damage = reader.ReadSingle();
      if (!bitField[2])
        return;
      DamageInterval = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, KillPlainLogic value)
    {
      value.Write(writer);
    }

    public static KillPlainLogic ReadRecord(BinaryReader reader)
    {
      var killPlainLogic = new KillPlainLogic();
      killPlainLogic.Read(reader);
      return killPlainLogic;
    }
}