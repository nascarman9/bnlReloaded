using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class GlobalExpLogic
{
    public ExpLogic? PlayerXp { get; set; }

    public ExpLogic? HeroXp { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(PlayerXp != null, HeroXp != null).Write(writer);
      if (PlayerXp != null)
        ExpLogic.WriteRecord(writer, PlayerXp);
      if (HeroXp != null)
        ExpLogic.WriteRecord(writer, HeroXp);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(2);
      bitField.Read(reader);
      PlayerXp = bitField[0] ? ExpLogic.ReadRecord(reader) : null;
      HeroXp = bitField[1] ? ExpLogic.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, GlobalExpLogic value)
    {
      value.Write(writer);
    }

    public static GlobalExpLogic ReadRecord(BinaryReader reader)
    {
      var globalExpLogic = new GlobalExpLogic();
      globalExpLogic.Read(reader);
      return globalExpLogic;
    }
}