using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class ResourceReward
{
    public float? PlayerReward { get; set; }

    public float? EnemyReward { get; set; }

    public float? TeamReward { get; set; }

    public bool Mining { get; set; }

    public void Write(BinaryWriter writer)
    {
      new BitField(PlayerReward.HasValue, EnemyReward.HasValue, TeamReward.HasValue, true).Write(writer);
      if (PlayerReward.HasValue)
        writer.Write(PlayerReward.Value);
      if (EnemyReward.HasValue)
        writer.Write(EnemyReward.Value);
      if (TeamReward.HasValue)
        writer.Write(TeamReward.Value);
      writer.Write(Mining);
    }

    public void Read(BinaryReader reader)
    {
      var bitField = new BitField(4);
      bitField.Read(reader);
      PlayerReward = bitField[0] ? reader.ReadSingle() : null;
      EnemyReward = bitField[1] ? reader.ReadSingle() : null;
      TeamReward = bitField[2] ? reader.ReadSingle() : null;
      if (!bitField[3])
        return;
      Mining = reader.ReadBoolean();
    }

    public static void WriteRecord(BinaryWriter writer, ResourceReward value)
    {
      value.Write(writer);
    }

    public static ResourceReward ReadRecord(BinaryReader reader)
    {
      var resourceReward = new ResourceReward();
      resourceReward.Read(reader);
      return resourceReward;
    }
}