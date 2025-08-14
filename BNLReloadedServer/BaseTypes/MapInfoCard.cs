using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class MapInfoCard : MapInfo
{
    public override MapInfoType Type => MapInfoType.Card;

    public Key MapKey { get; set; }

    public override void Write(BinaryWriter writer)
    {
        new BitField(true).Write(writer);
        Key.WriteRecord(writer, MapKey);
    }

    public override void Read(BinaryReader reader)
    {
        var bitField = new BitField(1);
        bitField.Read(reader);
        if (!bitField[0])
            return;
        MapKey = Key.ReadRecord(reader);
    }

    public static void WriteRecord(BinaryWriter writer, MapInfoCard value) => value.Write(writer);

    public static MapInfoCard ReadRecord(BinaryReader reader)
    {
        var mapInfoCard = new MapInfoCard();
        mapInfoCard.Read(reader);
        return mapInfoCard;
    }
}