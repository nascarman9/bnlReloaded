using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class HeroSwitchLogic
{
    public float ResourceCost { get; set; }

    public int? MaxPlayerLevel { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, MaxPlayerLevel.HasValue).Write(writer);
        writer.Write(ResourceCost);
        if (!MaxPlayerLevel.HasValue)
            return;
        writer.Write(MaxPlayerLevel.Value);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(2);
        bitField.Read(reader);
        if (bitField[0])
            ResourceCost = reader.ReadSingle();
        MaxPlayerLevel = bitField[1] ? reader.ReadInt32() : null;
    }

    public static void WriteRecord(BinaryWriter writer, HeroSwitchLogic value)
    {
        value.Write(writer);
    }

    public static HeroSwitchLogic ReadRecord(BinaryReader reader)
    {
        var heroSwitchLogic = new HeroSwitchLogic();
        heroSwitchLogic.Read(reader);
        return heroSwitchLogic;
    }
}