using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class LootItemUnit
{
    public Key LootUnitKey { get; set; }

    public RelativeTeamType KillerRelativeLootTeam { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, true).Write(writer);
        Key.WriteRecord(writer, LootUnitKey);
        writer.WriteByteEnum(KillerRelativeLootTeam);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            LootUnitKey = Key.ReadRecord(reader);
        if (!bitField[1])
            return;
        KillerRelativeLootTeam = reader.ReadByteEnum<RelativeTeamType>();
    }

    public static void WriteRecord(BinaryWriter writer, LootItemUnit value) => value.Write(writer);

    public static LootItemUnit ReadRecord(BinaryReader reader)
    {
        var lootItemUnit = new LootItemUnit();
        lootItemUnit.Read(reader);
        return lootItemUnit;
    }
}