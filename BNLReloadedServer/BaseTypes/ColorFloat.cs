namespace BNLReloadedServer.BaseTypes;

public struct ColorFloat(float r, float g, float b, float a) : IEquatable<ColorFloat>
{
    public float r = r;
    public float g = g;
    public float b = b;
    public float a = a;

    public ColorFloat(float r, float g, float b) : this(r, g, b, 1f)
    {
    }

    public static ColorFloat operator +(ColorFloat a, ColorFloat b)
    {
      return new ColorFloat(a.r + b.r, a.g + b.g, a.b + b.b, a.a + b.a);
    }

    public static ColorFloat operator -(ColorFloat a, ColorFloat b)
    {
      return new ColorFloat(a.r - b.r, a.g - b.g, a.b - b.b, a.a - b.a);
    }

    public static ColorFloat operator *(ColorFloat a, ColorFloat b)
    {
      return new ColorFloat(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);
    }

    public static ColorFloat operator *(ColorFloat a, float b)
    {
      return new ColorFloat(a.r * b, a.g * b, a.b * b, a.a * b);
    }

    public static ColorFloat operator *(float b, ColorFloat a)
    {
      return new ColorFloat(a.r * b, a.g * b, a.b * b, a.a * b);
    }

    public static ColorFloat operator /(ColorFloat a, float b)
    {
      return new ColorFloat(a.r / b, a.g / b, a.b / b, a.a / b);
    }

    public static bool operator ==(ColorFloat a, ColorFloat b)
    {
      return Equals(a.r, b.r) && Equals(a.g, b.g) && Equals(a.b, b.b) && Equals(a.a, b.a);
    }

    public static bool operator !=(ColorFloat a, ColorFloat b)
    {
      return !(a == b);
    }

    public bool Equals(ColorFloat other)
    {
      return r.Equals(other.r) && g.Equals(other.g) && b.Equals(other.b) && a.Equals(other.a);
    }

    public override bool Equals(object? obj)
    {
      return obj is ColorFloat other && Equals(other);
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(r, g, b, a);
    }
}