using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class PerkModEffect : PerkMod
{
    public override PerkModType Type => PerkModType.Effect;

    public List<Key>? Constant { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(Constant != null).Write(writer);
        if (Constant != null)
            writer.WriteList(Constant, Key.WriteRecord);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        Constant = bitField[0] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, PerkModEffect value) => value.Write(writer);

    public static PerkModEffect ReadRecord(BinaryReader reader)
    {
        var perkModEffect = new PerkModEffect();
        perkModEffect.Read(reader);
        return perkModEffect;
    }
}