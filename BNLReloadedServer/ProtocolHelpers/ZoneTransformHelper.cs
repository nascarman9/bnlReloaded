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

  extension(ZoneTransform t)
  {
    public bool IsCrouch() => t.IsCrouch;
    public void SetIsCrouch(bool value) => t.IsCrouch = value;
    public bool IsJump() => t.IsJump;
    public void SetIsJump(bool value) => t.IsJump = value;
    public bool IsSprint() => t.IsSprint;
    public void SetIsSprint(bool value) => t.IsSprint = value;
    public bool IsWallClimb() => t.IsWallClimb;
    public void SetIsWallClimb(bool value) => t.IsWallClimb = value;
    public bool NoInterpolation() => t.NoInterpolation;
    public void SetNoInterpolation(bool value) => t.NoInterpolation = value;
    public void SetPosition(Vector3 position) => t.Position = position;
    public Vector3 GetPosition() => t.Position;
    public void SetRotation(Quaternion q) => t.Rotation = ToVector3s(q);
    public Quaternion GetRotation() => ToQuaternion(t.Rotation);
    public void SetLocalVelocity(Vector3 v) => t.LocalVelocity = ToVector3s(v);
    public Vector3 GetLocalVelocity() => ToVector3(t.LocalVelocity);
  }

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
    Quaternion.CreateFromYawPitchRoll(float.DegreesToRadians(UnpackFromShort(rot.y)),
      float.DegreesToRadians(UnpackFromShort(rot.x)), float.DegreesToRadians(UnpackFromShort(rot.z)));

  public static Quaternion ToQuaternion(Vector2s rot) =>
    Quaternion.CreateFromYawPitchRoll(float.DegreesToRadians(UnpackFromShort(rot.y)), 0.0f, 0.0f) *
    Quaternion.CreateFromYawPitchRoll(0.0f, float.DegreesToRadians(UnpackFromShort(rot.x)), 0.0f);
  
  public static ZoneTransform ToZoneTransform(Vector3 pos, Quaternion rot) =>
    new()
    {
      Position = pos,
      Rotation = ToVector3s(rot)
    };
}