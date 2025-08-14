using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class PerkModAbility : PerkMod
{
    public override PerkModType Type => PerkModType.Ability;

    public Key ReplaceAbility { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true).Write(writer);
        Key.WriteRecord(writer, ReplaceAbility);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        if (!bitField[0])
            return;
        ReplaceAbility = Key.ReadRecord(reader);
    }

    public static void WriteRecord(BinaryWriter writer, PerkModAbility value)
    {
        value.Write(writer);
    }

    public static PerkModAbility ReadRecord(BinaryReader reader)
    {
        var perkModAbility = new PerkModAbility();
        perkModAbility.Read(reader);
        return perkModAbility;
    }
}