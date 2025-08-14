using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LootCrateMatchReward
{
    public Key? CrateKey { get; set; }

    public int Weight { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(CrateKey.HasValue, true).Write(writer);
        if (CrateKey.HasValue)
            Key.WriteRecord(writer, CrateKey.Value);
        writer.Write(Weight);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        CrateKey = bitField[0] ? Key.ReadRecord(reader) : null;
        if (!bitField[1])
            return;
        Weight = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, LootCrateMatchReward value)
    {
        value.Write(writer);
    }

    public static LootCrateMatchReward ReadRecord(BinaryReader reader)
    {
        var crateMatchReward = new LootCrateMatchReward();
        crateMatchReward.Read(reader);
        return crateMatchReward;
    }
}