using System.Numerics;
using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.ProtocolHelpers;

public static class Direction2dHelper
{
  public static Direction2D Calc(Vector3 from, Vector3 to) => Calc(to - from);

  public static Direction2D Calc(Vector3 direction)
  {
    if (!(direction != Vector3.Zero))
      return Direction2D.Front;
    var dictionary = new Dictionary<Direction2D, float>
    {
      {
        Direction2D.Front,
        direction.Z
      },
      {
        Direction2D.Back,
        -direction.Z
      },
      {
        Direction2D.Left,
        -direction.X
      },
      {
        Direction2D.Right,
        direction.X
      }
    };
    var key = Direction2D.Front;
    foreach (var keyValuePair in dictionary.Where(keyValuePair => keyValuePair.Value > (double) dictionary[key]))
    {
      key = keyValuePair.Key;
    }
    return key;
  }

  public static Vector3 Apply(Vector3 insidePoint, Direction2D dir) => insidePoint + ToVector3(dir);

  public static Quaternion Rotation(Direction2D dir) =>
    dir switch
    {
      Direction2D.Left => Quaternion.CreateFromAxisAngle(Vector3.UnitY, 3 * MathF.PI * 0.5f),
      Direction2D.Right => Quaternion.CreateFromAxisAngle(Vector3.UnitY,MathF.PI * 0.5f),
      Direction2D.Back => Quaternion.CreateFromAxisAngle(Vector3.UnitY,MathF.PI),
      _ => Quaternion.Identity
    };

  public static Quaternion RotationForWall(Direction2D dir) =>
    dir switch
    {
      Direction2D.Left => Quaternion.CreateFromAxisAngle(Vector3.UnitY, 3 * MathF.PI * 0.5f),
      Direction2D.Right => Quaternion.CreateFromAxisAngle(Vector3.UnitY,MathF.PI * 0.5f),
      Direction2D.Front => Quaternion.CreateFromAxisAngle(Vector3.UnitY,MathF.PI),
      _ => Quaternion.Identity
    };

  public static Vector3 ToVector3(Direction2D dir) =>
    dir switch
    {
      Direction2D.Left => Vector3s.Left.ToVector3(),
      Direction2D.Right => Vector3.UnitX,
      Direction2D.Back => Vector3s.Back.ToVector3(),
      Direction2D.Front => Vector3.UnitZ,
      _ => Vector3.Zero
    };

  public static Direction2D? FromVector3s(Vector3s dir) =>
    dir switch
    {
      { y: not 0 } => null,
      { x: > 0, z: 0 } => Direction2D.Right,
      { x: < 0, z: 0 } => Direction2D.Left,
      { z: > 0, x: 0 } => Direction2D.Front,
      _ => dir is { z: < 0, x: 0 } ? Direction2D.Back : null
    };

  public static Direction2D Inverse(this Direction2D dir) =>
    dir switch
    {
      Direction2D.Left => Direction2D.Right,
      Direction2D.Right => Direction2D.Left,
      Direction2D.Back => Direction2D.Front,
      Direction2D.Front => Direction2D.Back,
      _ => Direction2D.Front
    };
}