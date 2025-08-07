using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ServerUpdate
{
    public bool? PlayButtonEnabled { get; set; }

    public bool? RankedEnabled { get; set; }

    public bool? TutorialEnabled { get; set; }

    public bool? MadModeEnabled { get; set; }

    public bool? FriendlyEnabled { get; set; }

    public bool? ShopEnabled { get; set; }

    public bool? BuyPlatinumEnabled { get; set; }

    public bool? TimeAssaultEnabled { get; set; }

    public bool? MapEditorEnabled { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(PlayButtonEnabled.HasValue, RankedEnabled.HasValue, TutorialEnabled.HasValue, MadModeEnabled.HasValue, 
        FriendlyEnabled.HasValue, ShopEnabled.HasValue, BuyPlatinumEnabled.HasValue, TimeAssaultEnabled.HasValue, 
        MapEditorEnabled.HasValue).Write(writer);
      if (PlayButtonEnabled.HasValue)
        writer.Write(PlayButtonEnabled.Value);
      if (RankedEnabled.HasValue)
        writer.Write(RankedEnabled.Value);
      if (TutorialEnabled.HasValue)
        writer.Write(TutorialEnabled.Value);
      if (MadModeEnabled.HasValue)
        writer.Write(MadModeEnabled.Value);  
      if (FriendlyEnabled.HasValue)
        writer.Write(FriendlyEnabled.Value);
      if (ShopEnabled.HasValue)
        writer.Write(ShopEnabled.Value);
      if (BuyPlatinumEnabled.HasValue)
        writer.Write(BuyPlatinumEnabled.Value);
      if (TimeAssaultEnabled.HasValue)
        writer.Write(TimeAssaultEnabled.Value);
      if (!MapEditorEnabled.HasValue)
        return;
      writer.Write(MapEditorEnabled.Value);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(9);
      bitField.Read(reader);
      PlayButtonEnabled = !bitField[0] ? null : reader.ReadBoolean();
      RankedEnabled = !bitField[1] ? null : reader.ReadBoolean();
      TutorialEnabled = !bitField[2] ? null : reader.ReadBoolean();
      MadModeEnabled = !bitField[3] ? null : reader.ReadBoolean();
      FriendlyEnabled = !bitField[4] ? null : reader.ReadBoolean();
      ShopEnabled = !bitField[5] ? null : reader.ReadBoolean();
      BuyPlatinumEnabled = !bitField[6] ? null : reader.ReadBoolean();
      TimeAssaultEnabled = !bitField[7] ? null : reader.ReadBoolean();
      MapEditorEnabled = !bitField[8] ? null : reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, ServerUpdate value) => value.Write(writer);

    public static ServerUpdate ReadRecord(BinaryReader reader)
    {
      var serverUpdate = new ServerUpdate();
      serverUpdate.Read(reader);
      return serverUpdate;
    }
}