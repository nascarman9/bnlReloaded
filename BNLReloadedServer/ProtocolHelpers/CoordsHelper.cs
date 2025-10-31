using System.Numerics;
using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.ProtocolHelpers;

public static class CoordsHelper
{
  public static readonly BlockCorner[] AllCorners =
  [
    BlockCorner.C000,
    BlockCorner.C100,
    BlockCorner.C010,
    BlockCorner.C001,
    BlockCorner.C110,
    BlockCorner.C011,
    BlockCorner.C101,
    BlockCorner.C111
  ];
  public static readonly BlockCorner[] OppositeCorner =
  [
    BlockCorner.C111,
    BlockCorner.C011,
    BlockCorner.C101,
    BlockCorner.C110,
    BlockCorner.C001,
    BlockCorner.C100,
    BlockCorner.C010,
    BlockCorner.C000
  ];
  public static readonly Vector3s[] CornerToVertex =
  [
    Vector3s.Zero,
    Vector3s.Right,
    Vector3s.Up,
    Vector3s.Forward,
    new(1, 1, 0),
    new(0, 1, 1),
    new(1, 0, 1),
    Vector3s.One
  ];
  public static readonly BlockFace[] OppositeFace =
  [
    BlockFace.Bottom,
    BlockFace.Top,
    BlockFace.Left,
    BlockFace.Right,
    BlockFace.Back,
    BlockFace.Forward
  ];
  public static readonly Vector3s[] FaceToVector =
  [
    Vector3s.Up,
    Vector3s.Down,
    Vector3s.Right,
    Vector3s.Left,
    Vector3s.Forward,
    Vector3s.Back
  ];
  public static readonly Vector3[] FaceToNormal =
  [
    Vector3s.Up.ToVector3(),
    Vector3s.Down.ToVector3(),
    Vector3s.Right.ToVector3(),
    Vector3s.Left.ToVector3(),
    Vector3s.Forward.ToVector3(),
    Vector3s.Back.ToVector3()
  ];

  public static float Noise(Vector3s seed)
  {
    var num1 = seed.x + seed.y * 57 + seed.z * 1374;
    var num2 = num1 << 13 ^ num1;
    return MathF.Abs((float) (1.0 - (num2 * (num2 * num2 * 15731 + 789221) + 1376312589 & int.MaxValue) / 1073741824.0));
  }

  public static Vector3s ZoneToChunk(Vector3s zonePos) => zonePos / 12;

  public static Vector3 Floor(Vector3 origin) => new(MathF.Floor(origin.X), MathF.Floor(origin.Y), MathF.Floor(origin.Z));

  public static Vector3 Round(Vector3 origin) => new(MathF.Round(origin.X), MathF.Round(origin.Y), MathF.Round(origin.Z));

  public static Vector3 BlockCenter(Vector3 origin) => Floor(origin) + new Vector3(0.5f, 0.5f, 0.5f);

  public static Vector3 BlockCenter(Vector3s origin) => origin.ToVector3() + new Vector3(0.5f, 0.5f, 0.5f);

  public static Vector3 BlockBottom(Vector3 origin) => Floor(origin) + new Vector3(0.5f, 0.0f, 0.5f);
  
  public static Vector3 BlockBottom(Vector3s origin) => origin.ToVector3() + new Vector3(0.5f, 0.0f, 0.5f);

  public static BlockFace VectorToFace(Vector3s dir)
  {
    if (dir == Vector3s.Up)
      return BlockFace.Top;
    if (dir == Vector3s.Down)
      return BlockFace.Bottom;
    if (dir == Vector3s.Left)
      return BlockFace.Left;
    if (dir == Vector3s.Right)
      return BlockFace.Right;
    if (dir == Vector3s.Forward)
      return BlockFace.Forward;
    return dir == Vector3s.Back ? BlockFace.Back : BlockFace.Top;
  }

  public static BlockFace RotationToFace(Vector3s dir) =>
    Vector3.Transform(Vector3.UnitY, ZoneTransformHelper.ToQuaternion(dir)) switch
    {
      { Y: >= .95f } => BlockFace.Top,
      { Y: <= -.95f } => BlockFace.Bottom,
      { X: >= .95f } => BlockFace.Right,
      { X: <= -.95f } => BlockFace.Left,
      { Z: >= .95f } => BlockFace.Forward,
      { Z: <= -.95f } => BlockFace.Back,
      _ => BlockFace.Top
    };

  public static BlockCorner VectorToCorner(Vector3s dir)
  {
    if (dir == new Vector3s(0, 0, 0))
      return BlockCorner.C000;
    if (dir == new Vector3s(1, 0, 0))
      return BlockCorner.C100;
    if (dir == new Vector3s(0, 1, 0))
      return BlockCorner.C010;
    if (dir == new Vector3s(0, 0, 1))
      return BlockCorner.C001;
    if (dir == new Vector3s(1, 1, 0))
      return BlockCorner.C110;
    if (dir == new Vector3s(0, 1, 1))
      return BlockCorner.C011;
    if (dir == new Vector3s(1, 0, 1))
      return BlockCorner.C101;
    return dir == new Vector3s(1, 1, 1) ? BlockCorner.C111 : BlockCorner.C000;
  }

  public static Vector3s GetCollidingBlock(Vector3 collidingPoint)
  {
    var magCheckV = 2 * (collidingPoint - Vector3.Truncate(collidingPoint)) - Vector3.One;
    
    List<float> valList = [magCheckV.X, magCheckV.Y, magCheckV.Z];
    var magList = valList.Select(MathF.Abs).ToList();

    var magIndex = magList.IndexOf(magList.Max());

    var currBlock = (Vector3s)collidingPoint;
    return currBlock + magIndex switch
    {
      0 => MathF.Sign(valList[0]) < 0 ? Vector3s.Left : Vector3s.Right,
      1 => MathF.Sign(valList[1]) < 0 ? Vector3s.Down : Vector3s.Up,
      2 => MathF.Sign(valList[2]) < 0 ? Vector3s.Back : Vector3s.Forward,
      _ => Vector3s.Down,
    };
  }

  public static Vector3s NeighborChunkShift(Vector3s localPos) => new(localPos.x != 11 ? localPos.x != 0 ? 0 : -1 : 1,
    localPos.y != 11 ? localPos.y != 0 ? 0 : -1 : 1, localPos.z != 11 ? localPos.z != 0 ? 0 : -1 : 1);

  public static Vector3 Rotate(Vector3 v, float a)
  {
    var num1 = MathF.Sin(a);
    var num2 = MathF.Cos(a);
    var x = v.X;
    v.X = (float) (num2 * (double) x + num1 * (double) v.Z);
    v.Z = (float) (num2 * (double) v.Z - num1 * (double) x);
    return v;
  }

  private const double Sqrt3 = 1.7320508075689d;

  public static int MaxBlockTraversal(float radius) => (int)double.Ceiling(radius * Sqrt3);
}