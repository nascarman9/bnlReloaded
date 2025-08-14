using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MapEditorAudioInfo
{
    public LocalizedString? Name { get; set; }

    public string? Container { get; set; }

    public List<string>? Triggers { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(Name != null, Container != null, Triggers != null).Write(writer);
      if (Name != null)
        LocalizedString.WriteRecord(writer, Name);
      if (Container != null)
        writer.Write(Container);
      if (Triggers != null)
        writer.WriteList(Triggers, writer.Write);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      Name = bitField[0] ? LocalizedString.ReadRecord(reader) : null;
      Container = bitField[1] ? reader.ReadString() : null;
      Triggers = bitField[2] ? reader.ReadList<string, List<string>>(reader.ReadString) : null;
    }

    public static void WriteRecord(BinaryWriter writer, MapEditorAudioInfo value)
    {
      value.Write(writer);
    }

    public static MapEditorAudioInfo ReadRecord(BinaryReader reader)
    {
      var mapEditorAudioInfo = new MapEditorAudioInfo();
      mapEditorAudioInfo.Read(reader);
      return mapEditorAudioInfo;
    }
}