using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class EffectGuiInfo
{
    public LocalizedString? Name { get; set; }

    public LocalizedString? Description { get; set; }

    public string? Icon { get; set; }

    public EffectGuiIconPosType? IconPosType { get; set; }

    public EffectGuiIconFormType? IconFormType { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(Name != null, Description != null, Icon != null, IconPosType.HasValue, IconFormType.HasValue).Write(writer);
      if (Name != null)
        LocalizedString.WriteRecord(writer, Name);
      if (Description != null)
        LocalizedString.WriteRecord(writer, Description);
      if (Icon != null)
        writer.Write(Icon);
      if (IconPosType.HasValue)
        writer.WriteByteEnum(IconPosType.Value);
      if (!IconFormType.HasValue)
        return;
      writer.WriteByteEnum(IconFormType.Value);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(5);
      bitField.Read(reader);
      Name = bitField[0] ? LocalizedString.ReadRecord(reader) : null;
      Description = bitField[1] ? LocalizedString.ReadRecord(reader) : null;
      Icon = bitField[2] ? reader.ReadString() : null;
      IconPosType = bitField[3] ? reader.ReadByteEnum<EffectGuiIconPosType>() : null;
      IconFormType = bitField[4] ? reader.ReadByteEnum<EffectGuiIconFormType>() : null;
    }

    public static void WriteRecord(BinaryWriter writer, EffectGuiInfo value) => value.Write(writer);

    public static EffectGuiInfo ReadRecord(BinaryReader reader)
    {
      var effectGuiInfo = new EffectGuiInfo();
      effectGuiInfo.Read(reader);
      return effectGuiInfo;
    }
}