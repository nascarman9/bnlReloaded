using System.Numerics;
using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class EMapValidation : Exception
{
    public string? Error { get; set; }

    public TeamType? Team { get; set; }

    public Vector3? Position { get; set; }

    public Key? Card { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(Error != null, Team.HasValue, Position.HasValue, Card.HasValue).Write(writer);
        if (Error != null)
            writer.Write(Error);
        if (Team.HasValue)
            writer.WriteByteEnum(Team.Value);
        if (Position.HasValue)
            writer.Write(Position.Value);
        if (!Card.HasValue)
            return;
        Key.WriteRecord(writer, Card.Value);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(4);
        bitField.Read(reader);
        Error = bitField[0] ? reader.ReadString() : null;
        Team = bitField[1] ? reader.ReadByteEnum<TeamType>() : null;
        Position = bitField[2] ? reader.ReadVector3() : null;
        Card = bitField[3] ? Key.ReadRecord(reader) : null;
    }

    public static void WriteRecord(BinaryWriter writer, EMapValidation value)
    {
        value.Write(writer);
    }

    public static EMapValidation ReadRecord(BinaryReader reader)
    {
        var emapValidation = new EMapValidation();
        emapValidation.Read(reader);
        return emapValidation;
    }
}