using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class HeroTip
{
    public string? Image { get; set; }

    public LocalizedString? TipText { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(Image != null, TipText != null).Write(writer);
        if (Image != null)
            writer.Write(Image);
        if (TipText != null)
            LocalizedString.WriteRecord(writer, TipText);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        Image = bitField[0] ? reader.ReadString() : null;
        TipText = bitField[1] ? LocalizedString.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, HeroTip value) => value.Write(writer);

    public static HeroTip ReadRecord(BinaryReader reader)
    {
        var heroTip = new HeroTip();
        heroTip.Read(reader);
        return heroTip;
    }
}