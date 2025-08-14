using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class AbilityGuiInfo
{
    public string? Icon { get; set; }

    public LocalizedString? Name { get; set; }

    public LocalizedString? Description { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(Icon != null, Name != null, Description != null).Write(writer);
      if (Icon != null)
        writer.Write(Icon);
      if (Name != null)
        LocalizedString.WriteRecord(writer, Name);
      if (Description != null)
        LocalizedString.WriteRecord(writer, Description);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(3);
      bitField.Read(reader);
      Icon = bitField[0] ? reader.ReadString() : null;
      Name = bitField[1] ? LocalizedString.ReadRecord(reader) : null;
      Description = bitField[2] ? LocalizedString.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, AbilityGuiInfo value)
    {
      value.Write(writer);
    }

    public static AbilityGuiInfo ReadRecord(BinaryReader reader)
    {
      var abilityGuiInfo = new AbilityGuiInfo();
      abilityGuiInfo.Read(reader);
      return abilityGuiInfo;
    }
}