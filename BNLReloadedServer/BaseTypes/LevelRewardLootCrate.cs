using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LevelRewardLootCrate : LevelReward
{
    public override LevelRewardType Type => LevelRewardType.LootCrate;

    public Key CrateKey { get; set; }

    public int Amount { get; set; } = 1;

    public override void Write(BinaryWriter writer)
    {
        new BitField(true, true).Write(writer);
        Key.WriteRecord(writer, CrateKey);
        writer.Write(Amount);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            CrateKey = Key.ReadRecord(reader);
        if (!bitField[1])
            return;
        Amount = reader.ReadInt32();
    }

    public static void WriteRecord(BinaryWriter writer, LevelRewardLootCrate value)
    {
        value.Write(writer);
    }

    public static LevelRewardLootCrate ReadRecord(BinaryReader reader)
    {
        var levelRewardLootCrate = new LevelRewardLootCrate();
        levelRewardLootCrate.Read(reader);
        return levelRewardLootCrate;
    }
}