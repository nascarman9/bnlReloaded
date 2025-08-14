using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class AbilityCharges
{
    public int MaxCharges { get; set; }

    public float ChargeCooldown { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true).Write(writer);
        writer.Write(MaxCharges);
        writer.Write(ChargeCooldown);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            MaxCharges = reader.ReadInt32();
        if (!bitField[1])
            return;
        ChargeCooldown = reader.ReadSingle();
    }

    public static void WriteRecord(BinaryWriter writer, AbilityCharges value)
    {
        value.Write(writer);
    }

    public static AbilityCharges ReadRecord(BinaryReader reader)
    {
        var abilityCharges = new AbilityCharges();
        abilityCharges.Read(reader);
        return abilityCharges;
    }
}