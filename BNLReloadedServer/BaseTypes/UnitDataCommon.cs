using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class UnitDataCommon : UnitData
{
    public override UnitType Type => UnitType.Common;

    public override void Write(BinaryWriter writer) => new BitField().Write(writer);

    public override void Read(BinaryReader reader) => new BitField(0).Read(reader);

    public static void WriteRecord(BinaryWriter writer, UnitDataCommon value)
    {
        value.Write(writer);
    }

    public static UnitDataCommon ReadRecord(BinaryReader reader)
    {
        var unitDataCommon = new UnitDataCommon();
        unitDataCommon.Read(reader);
        return unitDataCommon;
    }
}