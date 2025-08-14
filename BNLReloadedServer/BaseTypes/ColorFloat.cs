namespace BNLReloadedServer.BaseTypes;

public struct ColorFloat(float r, float g, float b, float a)
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
}