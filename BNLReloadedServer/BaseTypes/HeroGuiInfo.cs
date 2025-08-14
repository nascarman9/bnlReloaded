using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class HeroGuiInfo
{
    public string? IconPortrait { get; set; }

    public string? IconPortraitBackground { get; set; }

    public string? IconPortraitProfile { get; set; }

    public string? IconPortraitLevelup { get; set; }

    public LocalizedString? Description { get; set; }

    public LocalizedString? LongDescription { get; set; }

    public string? LearningVideo { get; set; }

    public LocalizedString? Message { get; set; }

    public LocalizedString? Focus { get; set; }

    public HeroDifficultyType Difficulty { get; set; }

    public float Toughness { get; set; }

    public float Mobility { get; set; }

    public float Construction { get; set; }

    public AbilityGuiInfo? PassiveAbility { get; set; }

    public AbilityGuiInfo? ActiveAbility { get; set; }

    public string? SteamGuideLink { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(IconPortrait != null, IconPortraitBackground != null, IconPortraitProfile != null,
        IconPortraitLevelup != null, Description != null, LongDescription != null, LearningVideo != null,
        Message != null, Focus != null, true, true, true, true, PassiveAbility != null, ActiveAbility != null,
        SteamGuideLink != null).Write(writer);
      if (IconPortrait != null)
        writer.Write(IconPortrait);
      if (IconPortraitBackground != null)
        writer.Write(IconPortraitBackground);
      if (IconPortraitProfile != null)
        writer.Write(IconPortraitProfile);
      if (IconPortraitLevelup != null)
        writer.Write(IconPortraitLevelup);
      if (Description != null)
        LocalizedString.WriteRecord(writer, Description);
      if (LongDescription != null)
        LocalizedString.WriteRecord(writer, LongDescription);
      if (LearningVideo != null)
        writer.Write(LearningVideo);
      if (Message != null)
        LocalizedString.WriteRecord(writer, Message);
      if (Focus != null)
        LocalizedString.WriteRecord(writer, Focus);
      writer.WriteByteEnum(Difficulty);
      writer.Write(Toughness);
      writer.Write(Mobility);
      writer.Write(Construction);
      if (PassiveAbility != null)
        AbilityGuiInfo.WriteRecord(writer, PassiveAbility);
      if (ActiveAbility != null)
        AbilityGuiInfo.WriteRecord(writer, ActiveAbility);
      if (SteamGuideLink == null)
        return;
      writer.Write(SteamGuideLink);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(16);
      bitField.Read(reader);
      IconPortrait = bitField[0] ? reader.ReadString() : null;
      IconPortraitBackground = bitField[1] ? reader.ReadString() : null;
      IconPortraitProfile = bitField[2] ? reader.ReadString() : null;
      IconPortraitLevelup = bitField[3] ? reader.ReadString() : null;
      Description = bitField[4] ? LocalizedString.ReadRecord(reader) : null;
      LongDescription = bitField[5] ? LocalizedString.ReadRecord(reader) : null;
      LearningVideo = bitField[6] ? reader.ReadString() : null;
      Message = bitField[7] ? LocalizedString.ReadRecord(reader) : null;
      Focus = bitField[8] ? LocalizedString.ReadRecord(reader) : null;
      if (bitField[9])
        Difficulty = reader.ReadByteEnum<HeroDifficultyType>();
      if (bitField[10])
        Toughness = reader.ReadSingle();
      if (bitField[11])
        Mobility = reader.ReadSingle();
      if (bitField[12])
        Construction = reader.ReadSingle();
      PassiveAbility = bitField[13] ? AbilityGuiInfo.ReadRecord(reader) : null;
      ActiveAbility = bitField[14] ? AbilityGuiInfo.ReadRecord(reader) : null;
      SteamGuideLink = bitField[15] ? reader.ReadString() : null;
    }

    public static void WriteRecord(BinaryWriter writer, HeroGuiInfo value) => value.Write(writer);

    public static HeroGuiInfo ReadRecord(BinaryReader reader)
    {
      var heroGuiInfo = new HeroGuiInfo();
      heroGuiInfo.Read(reader);
      return heroGuiInfo;
    }
}