using System.Numerics;

namespace BNLReloadedServer.BaseTypes;

[Serializable]
public struct Vector3s(short x, short y, short z) : IEquatable<Vector3s>
{
  public short x = x;
  public short y = y;
  public short z = z;

  public Vector3s(float x, float y, float z) : this((short) Math.Floor(x), (short) Math.Floor(y), (short) Math.Floor(z))
  {
  }

  public Vector3s(int x, int y, int z) : this((short) x, (short) y, (short) z)
  {
  }

  public static Vector3s One => new(1, 1, 1);

  public static Vector3s Zero => new(0, 0, 0);

  public static Vector3s Right => new(1, 0, 0);

  public static Vector3s Left => new(-1, 0, 0);

  public static Vector3s Up => new(0, 1, 0);

  public static Vector3s Down => new(0, -1, 0);

  public static Vector3s Forward => new(0, 0, 1);

  public static Vector3s Back => new(0, 0, -1);

  public static Vector3s Cross(Vector3s rhs, Vector3s lhs)
  {
    return new Vector3s(rhs.y * lhs.z - rhs.z * lhs.y, rhs.z * lhs.x - rhs.x * lhs.z, rhs.x * lhs.y - rhs.y * lhs.x);
  }

  public Vector3 ToVector3() => new(x, y, z);

  public uint To10BitU32()
  {
    var xPos = x & ((1 << 10) - 1);
    var yPos = (y & ((1 << 10) - 1)) << 10;
    var zPos = (z & ((1 << 10) - 1)) << 20;
    return (uint)(zPos | yPos | xPos);
  }

  public static Vector3s From10BitU32(uint bits)
  {
    var z = bits >> 20;
    var y = bits >> 10 & ((1 << 10) - 1);
    var x = bits & ((1 << 10) - 1);
    return new Vector3s((short)bits, (short)bits, (short)bits);
  }

  public override bool Equals(object? other)
  {
    return other is Vector3s vector3S ? this == vector3S : base.Equals(other);
  }

  public override int GetHashCode() => x << 16 ^ y << 8 ^ z;

  public override string ToString()
  {
    return x + "," + y+ "," + z;
  }

  public static explicit operator Vector3s(Vector3 v) => new(v.X, v.Y, v.Z);

  public static Vector3s operator *(int d, Vector3s a)
  {
    return new Vector3s(a.x * d, a.y * d, a.z * d);
  }

  public static Vector3s operator *(Vector3s a, int d)
  {
    return new Vector3s(a.x * d, a.y * d, a.z * d);
  }

  public static Vector3s operator /(Vector3s a, int d)
  {
    return new Vector3s(a.x / d, a.y / d, a.z / d);
  }

  public static Vector3s operator -(Vector3s v) => new(-v.x, -v.y, -v.z);

  public static Vector3s operator -(Vector3s left, Vector3s right)
  {
    return new Vector3s(left.x - right.x, left.y - right.y, left.z - right.z);
  }

  public static Vector3s operator +(Vector3s left, Vector3s right)
  {
    return new Vector3s(left.x + right.x, left.y + right.y, left.z + right.z);
  }

  public static bool operator !=(Vector3s left, Vector3s right)
  {
    return left.x != right.x || left.y != right.y || left.z != right.z;
  }

  public static bool operator ==(Vector3s left, Vector3s right)
  {
    return left.x == right.x && left.y == right.y && left.z == right.z;
  }

  public bool Equals(Vector3s other)
  {
    return x == other.x && y == other.y && z == other.z;
  }
}