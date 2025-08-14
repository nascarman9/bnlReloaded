using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class Health
{
    public float MaxHealth { get; set; }

    public float Toughness { get; set; }

    public float Shield { get; set; }

    public HealthType HealthType { get; set; } = HealthType.World;

    public bool MiningOnly { get; set; }

    public bool MeleeOnly { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true, true, true).Write(writer);
      writer.Write(MaxHealth);
      writer.Write(Toughness);
      writer.Write(Shield);
      writer.WriteByteEnum(HealthType);
      writer.Write(MiningOnly);
      writer.Write(MeleeOnly);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(6);
      bitField.Read(reader);
      if (bitField[0])
        MaxHealth = reader.ReadSingle();
      if (bitField[1])
        Toughness = reader.ReadSingle();
      if (bitField[2])
        Shield = reader.ReadSingle();
      if (bitField[3])
        HealthType = reader.ReadByteEnum<HealthType>();
      if (bitField[4])
        MiningOnly = reader.ReadBoolean();
      if (!bitField[5])
        return;
      MeleeOnly = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, Health value) => value.Write(writer);

    public static Health ReadRecord(BinaryReader reader)
    {
      var health = new Health();
      health.Read(reader);
      return health;
    }
}