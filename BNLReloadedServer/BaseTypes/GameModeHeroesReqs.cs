using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class GameModeHeroesReqs
{
    public int? MinHeroesAvailable { get; set; }

    public int? MinHeroLevel { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(MinHeroesAvailable.HasValue, MinHeroLevel.HasValue).Write(writer);
      if (MinHeroesAvailable.HasValue)
        writer.Write(MinHeroesAvailable.Value);
      if (!MinHeroLevel.HasValue)
        return;
      writer.Write(MinHeroLevel.Value);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(2);
      bitField.Read(reader);
      MinHeroesAvailable = bitField[0] ? reader.ReadInt32() : null;
      MinHeroLevel = bitField[1] ? reader.ReadInt32() : null;
    }

    public static void WriteRecord(BinaryWriter writer, GameModeHeroesReqs value)
    {
      value.Write(writer);
    }

    public static GameModeHeroesReqs ReadRecord(BinaryReader reader)
    {
      var gameModeHeroesReqs = new GameModeHeroesReqs();
      gameModeHeroesReqs.Read(reader);
      return gameModeHeroesReqs;
    }
}