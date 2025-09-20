using System.Numerics;
using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.ProtocolHelpers;

public static class ZoneTransformHelper
{
  private const byte BitIsCrouch = 1;
  private const byte BitIsJump = 2;
  private const byte BitIsSprint = 3;
  private const byte BitIsWallClimb = 4;
  private const byte BitNoInterpolation = 5;
  private static byte _flags;

  public static bool IsCrouch(this ZoneTransform t) => t.IsCrouch;

  public static void SetIsCrouch(this ZoneTransform t, bool value) => t.IsCrouch = value;

  public static bool IsJump(this ZoneTransform t) => t.IsJump;

  public static void SetIsJump(this ZoneTransform t, bool value) => t.IsJump = value;

  public static bool IsSprint(this ZoneTransform t) => t.IsSprint;

  public static void SetIsSprint(this ZoneTransform t, bool value) => t.IsSprint = value;

  public static bool IsWallClimb(this ZoneTransform t) => t.IsWallClimb;

  public static void SetIsWallClimb(this ZoneTransform t, bool value) => t.IsWallClimb = value;

  public static bool NoInterpolation(this ZoneTransform t) => t.NoInterpolation;

  public static void SetNoInterpolation(this ZoneTransform t, bool value) => t.NoInterpolation = value;

  private static bool GetFlag(int bitNum) => (_flags & 1 << bitNum) > 0;

  private static void SetFlag(int bitNum, bool val)
  {
    if (val)
      _flags |= (byte) (1 << bitNum);
    else
      _flags &= (byte) ~(1 << bitNum);
  }

  public static bool CompareTransforms(ZoneTransform? t1, ZoneTransform? t2) =>
    t1 == t2 || (t1 != null && t2 != null &&
                 Vector3.Distance(t1.Position, t2.Position) < 9.9999997473787516E-05 &&
                 t1.Rotation == t2.Rotation && t1.LocalVelocity == t2.LocalVelocity && t1.IsCrouch == t2.IsCrouch &&
                 t1.IsJump == t2.IsJump && t1.IsSprint == t2.IsSprint && t1.IsWallClimb == t2.IsWallClimb &&
                 t1.IsDash == t2.IsDash && t1.IsGroundSlam == t2.IsGroundSlam && t1.NoInterpolation == t2.NoInterpolation);

  public static void SetPosition(this ZoneTransform t, Vector3 position) => t.Position = position;

  public static Vector3 GetPosition(this ZoneTransform t) => t.Position;

  public static void SetRotation(this ZoneTransform t, Quaternion q) => t.Rotation = ToVector3s(q);

  public static Quaternion GetRotation(this ZoneTransform t) => ToQuaternion(t.Rotation);

  public static void SetLocalVelocity(this ZoneTransform t, Vector3 v) => t.LocalVelocity = ToVector3s(v);

  public static Vector3 GetLocalVelocity(this ZoneTransform t) => ToVector3(t.LocalVelocity);

  public static short PackToShort(float v) => (short) MathF.Round(v * 10f);

  public static float UnpackFromShort(short v) => v / 10f;

  public static Vector3 ToVector3(Vector3s pos) => new(UnpackFromShort(pos.x), UnpackFromShort(pos.y), UnpackFromShort(pos.z));

  public static Vector3s ToVector3s(Vector3 pos) => new(PackToShort(pos.X), PackToShort(pos.Y), PackToShort(pos.Z));

  public static Vector3s ToVector3s(Quaternion rot)
  {
    var eulerAngles = Vector3.RadiansToDegrees(rot.ToEulerAngles());
    return new Vector3s(PackToShort(eulerAngles.X), PackToShort(eulerAngles.Y), PackToShort(eulerAngles.Z));
  }

  public static Quaternion ToQuaternion(Vector3s rot) =>
    Quaternion.CreateFromYawPitchRoll(float.DegreesToRadians(UnpackFromShort(rot.x)), float.DegreesToRadians(UnpackFromShort(rot.y)), float.DegreesToRadians(UnpackFromShort(rot.z)));

  public static Quaternion ToQuaternion(Vector2s rot) =>
    Quaternion.CreateFromYawPitchRoll(0.0f, float.DegreesToRadians(UnpackFromShort(rot.y)), 0.0f) *
    Quaternion.CreateFromYawPitchRoll(float.DegreesToRadians(UnpackFromShort(rot.x)), 0.0f, 0.0f);
  
  public static ZoneTransform ToZoneTransform(Vector3 pos, Quaternion rot) =>
    new()
    {
      Position = pos,
      Rotation = ToVector3s(rot)
    };
}