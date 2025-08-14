using System.Numerics;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class AbilityCastData
{
    public Key AbilityKey { get; set; }

    public Vector3? ShotPos { get; set; }

    public List<ShotData>? Shots { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(true, ShotPos.HasValue, Shots != null).Write(writer);
        Key.WriteRecord(writer, AbilityKey);
        if (ShotPos.HasValue)
            writer.Write(ShotPos.Value);
        if (Shots != null)
            writer.WriteList(Shots, ShotData.WriteRecord);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        if (bitField[0])
            AbilityKey = Key.ReadRecord(reader);
        ShotPos = bitField[1] ? reader.ReadVector3() : null;
        Shots = bitField[2] ? reader.ReadList<ShotData, List<ShotData>>(ShotData.ReadRecord) : null;
    }

    public static void WriteRecord(BinaryWriter writer, AbilityCastData value)
    {
        value.Write(writer);
    }

    public static AbilityCastData ReadRecord(BinaryReader reader)
    {
        var abilityCastData = new AbilityCastData();
        abilityCastData.Read(reader);
        return abilityCastData;
    }
}