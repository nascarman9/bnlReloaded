using System.Numerics;

namespace BNLReloadedServer.BaseTypes;

[Serializable]
public struct Vector2s(short x, short y) : IEquatable<Vector2s>
{
  public short x = x;
  public short y = y;

  public Vector2s(float x, float y) : this((short) x, (short) y)
  {
  }

  public Vector2s(int x, int y) : this((short) x, (short) y)
  {
  }

  public static Vector2s One => new(1, 1);

  public static Vector2s Zero => new(0, 0);

  public static Vector2s Right => new(1, 0);

  public static Vector2s Up => new(0, 1);

  public Vector2 ToVector2() => new(x, y);

  public override bool Equals(object? other)
  {
    return other is Vector2s vector2S ? this == vector2S : base.Equals(other);
  }

  public override int GetHashCode() => x << 16 ^ y;

  public override string ToString() => $"({x},{y})";

  public static explicit operator Vector2s(Vector2 v) => new Vector2s(v.X, v.Y);

  public static Vector2s operator *(int d, Vector2s a)
  {
    return new Vector2s(a.x * d, a.y * d);
  }

  public static Vector2s operator *(Vector2s a, int d)
  {
    return new Vector2s(a.x * d, a.y * d);
  }

  public static Vector2s operator /(Vector2s a, int d)
  {
    return new Vector2s(a.x / d, a.y / d);
  }

  public static Vector2s operator -(Vector2s v) => new(-v.x, -v.y);

  public static Vector2s operator -(Vector2s left, Vector2s right)
  {
    return new Vector2s(left.x - right.x, left.y - right.y);
  }

  public static Vector2s operator +(Vector2s left, Vector2s right)
  {
    return new Vector2s(left.x + right.x, left.y + right.y);
  }

  public static bool operator !=(Vector2s left, Vector2s right)
  {
    return left.x != right.x || left.y != right.y;
  }

  public static bool operator ==(Vector2s left, Vector2s right)
  {
    return left.x == right.x && left.y == right.y;
  }

  public bool Equals(Vector2s other)
  {
    return x == other.x && y == other.y;
  }
}