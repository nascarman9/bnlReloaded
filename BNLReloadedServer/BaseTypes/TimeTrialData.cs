using BNLReloadedServer.ProtocolHelpers;

namespace BNLReloadedServer.BaseTypes;

public class TimeTrialData
{
    public Dictionary<Key, List<int>>? CompletedGoals { get; set; }

    public Dictionary<Key, float>? BestResultTime { get; set; }

    public ulong ResetTime { get; set; }

    public void Write(BinaryWriter writer)
    {
        new BitField(CompletedGoals != null, BestResultTime != null, true).Write(writer);
        if (CompletedGoals != null)
            writer.WriteMap(CompletedGoals, Key.WriteRecord, item => writer.WriteList(item, writer.Write));
        if (BestResultTime != null)
            writer.WriteMap(BestResultTime, Key.WriteRecord, writer.Write);
        writer.Write(ResetTime);
    }

    public void Read(BinaryReader reader)
    {
        var bitField = new BitField(3);
        bitField.Read(reader);
        CompletedGoals = bitField[0] ? reader.ReadMap<Key, List<int>, Dictionary<Key, List<int>>>(Key.ReadRecord, () => reader.ReadList<int, List<int>>(reader.ReadInt32)) : null;
        BestResultTime = bitField[1] ? reader.ReadMap<Key, float, Dictionary<Key, float>>(Key.ReadRecord, reader.ReadSingle) : null;
        if (!bitField[2])
            return;
        ResetTime = reader.ReadUInt64();
    }

    public static void WriteRecord(BinaryWriter writer, TimeTrialData value) => value.Write(writer);

    public static TimeTrialData ReadRecord(BinaryReader reader)
    {
        var timeTrialData = new TimeTrialData();
        timeTrialData.Read(reader);
        return timeTrialData;
    }
    
    public static byte[] WriteByteRecord(TimeTrialData timeTrialData)
    {
        var memStream = new MemoryStream();
        using var writer = new BinaryWriter(memStream);
        WriteRecord(writer, timeTrialData);
        return memStream.ToArray();
    }

    public static TimeTrialData ReadByteRecord(byte[] bytes)
    {
        var memStream = new MemoryStream(bytes);
        using var reader = new BinaryReader(memStream);
        return ReadRecord(reader);
    }
}