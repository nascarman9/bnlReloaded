using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class PerkModPassive : PerkMod
{
    public override PerkModType Type => PerkModType.Passive;

    public List<Key>? ReplaceEffects { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(ReplaceEffects != null).Write(writer);
        if (ReplaceEffects != null)
            writer.WriteList(ReplaceEffects, Key.WriteRecord);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        ReplaceEffects = bitField[0] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, PerkModPassive value)
    {
        value.Write(writer);
    }

    public static PerkModPassive ReadRecord(BinaryReader reader)
    {
        var perkModPassive = new PerkModPassive();
        perkModPassive.Read(reader);
        return perkModPassive;
    }
}