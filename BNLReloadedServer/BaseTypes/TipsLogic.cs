using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class TipsLogic
{
    public int ChangeInterval { get; set; }

    public List<LocalizedString>? Tips { get; set; }

    public Dictionary<Key, List<HeroTip>>? SpecificHeroTips { get; set; }

    public List<HeroTip>? GeneralHeroTips { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, Tips != null, SpecificHeroTips != null, GeneralHeroTips != null).Write(writer);
      writer.Write(ChangeInterval);
      if (Tips != null)
        writer.WriteList(Tips, LocalizedString.WriteRecord);
      if (SpecificHeroTips != null)
        writer.WriteMap(SpecificHeroTips, Key.WriteRecord, item => writer.WriteList(item, HeroTip.WriteRecord));
      if (GeneralHeroTips != null)
        writer.WriteList(GeneralHeroTips, HeroTip.WriteRecord);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      if (bitField[0])
        ChangeInterval = reader.ReadInt32();
      Tips = bitField[1] ? reader.ReadList<LocalizedString, List<LocalizedString>>(LocalizedString.ReadRecord) : null;
      SpecificHeroTips = bitField[2] ? reader.ReadMap<Key, List<HeroTip>, Dictionary<Key, List<HeroTip>>>(Key.ReadRecord, () => reader.ReadList<HeroTip, List<HeroTip>>(HeroTip.ReadRecord)) : null;
      GeneralHeroTips = bitField[3] ? reader.ReadList<HeroTip, List<HeroTip>>(HeroTip.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, TipsLogic value) => value.Write(writer);

    public static TipsLogic ReadRecord(BinaryReader reader)
    {
      var tipsLogic = new TipsLogic();
      tipsLogic.Read(reader);
      return tipsLogic;
    }
}