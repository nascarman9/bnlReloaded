using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class Damage
{
    public float PlayerDamage { get; set; }

    public float WorldDamage { get; set; }

    public float ObjectiveDamage { get; set; }

    public bool Mining { get; set; }

    public bool Melee { get; set; }

    public bool IgnoreInvincibility { get; set; }

    public bool IgnoreDefences { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, true, true, true, true, true).Write(writer);
      writer.Write(PlayerDamage);
      writer.Write(WorldDamage);
      writer.Write(ObjectiveDamage);
      writer.Write(Mining);
      writer.Write(Melee);
      writer.Write(IgnoreInvincibility);
      writer.Write(IgnoreDefences);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(7);
      bitField.Read(reader);
      if (bitField[0])
        PlayerDamage = reader.ReadSingle();
      if (bitField[1])
        WorldDamage = reader.ReadSingle();
      if (bitField[2])
        ObjectiveDamage = reader.ReadSingle();
      if (bitField[3])
        Mining = reader.ReadBoolean();
      if (bitField[4])
        Melee = reader.ReadBoolean();
      if (bitField[5])
        IgnoreInvincibility = reader.ReadBoolean();
      if (!bitField[6])
        return;
      IgnoreDefences = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, Damage value) => value.Write(writer);

    public static Damage ReadRecord(BinaryReader reader)
    {
      var damage = new Damage();
      damage.Read(reader);
      return damage;
    }
}