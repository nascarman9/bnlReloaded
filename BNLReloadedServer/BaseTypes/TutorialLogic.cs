using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class TutorialLogic
{
    public Key GameMode { get; set; }

    public Key DefaultHero { get; set; }

    public List<Key>? DefaultDevices { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(true, true, DefaultDevices != null).Write(writer);
      Key.WriteRecord(writer, GameMode);
      Key.WriteRecord(writer, DefaultHero);
      if (DefaultDevices != null)
        writer.WriteList(DefaultDevices, Key.WriteRecord);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      if (bitField[0])
        GameMode = Key.ReadRecord(reader);
      if (bitField[1])
        DefaultHero = Key.ReadRecord(reader);
      DefaultDevices = bitField[2] ? reader.ReadList<Key, List<Key>>(Key.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, TutorialLogic value) => value.Write(writer);

    public static TutorialLogic ReadRecord(BinaryReader reader)
    {
      var tutorialLogic = new TutorialLogic();
      tutorialLogic.Read(reader);
      return tutorialLogic;
    }
}