namespace BNLReloadedServer.BaseTypes;

public struct Tick(int value) : IComparable<Tick>
{
    public const float Delta = 0.1f;
    public const int DeltaMillis = 100;
    private int value = value;
    public static Tick Current;
    public static float Portion;
    public static Tick Last;
    public static bool IsNew;

    public long TimeMillis => value * 100L;

    public double PreciseTime => value * 0.1;

    public void Read(BinaryReader reader) => value = reader.ReadInt32();

    public void Write(BinaryWriter writer) => writer.Write(value);

    public override string ToString() => $"tick{value}";

    public override int GetHashCode() => value;

    public override bool Equals(object? obj) => obj is Tick tick && value == tick.value;

    public int CompareTo(Tick other)
    {
        if (value < other.value)
            return -1;
        return value > other.value ? 1 : 0;
    }

    public static bool operator ==(Tick a, Tick b) => a.value == b.value;

    public static bool operator !=(Tick a, Tick b) => a.value != b.value;

    public static bool operator <(Tick a, Tick b) => a.value < b.value;

    public static bool operator <=(Tick a, Tick b) => a.value <= b.value;

    public static bool operator >=(Tick a, Tick b) => a.value >= b.value;

    public static bool operator >(Tick a, Tick b) => a.value > b.value;

    public static int operator -(Tick a, Tick b) => a.value - b.value;

    public static Tick operator +(Tick a, int b) => new Tick(a.value + b);

    public static Tick operator -(Tick a, int b) => new Tick(a.value - b);

    public static Tick operator ++(Tick a) => new Tick(a.value + 1);

    public static Tick operator --(Tick a) => new Tick(a.value - 1);

    public static Tick operator %(Tick a, int b) => new Tick(a.value % b);
}