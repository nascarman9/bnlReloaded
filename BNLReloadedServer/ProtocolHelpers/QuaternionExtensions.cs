using System.Collections.Specialized;
using System.Numerics;
using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.ProtocolHelpers;

public static class QuaternionExtensions
{
    public static Vector3 ToEulerAngles(this Quaternion q)
    {
        var angles = new Vector3();

        // Roll (rotation around X-axis)
        var sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
        var cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
        angles.X = MathF.Atan2(sinr_cosp, cosr_cosp);

        // Pitch (rotation around Y-axis)
        var sinp = 2 * (q.W * q.Y - q.Z * q.X);
        angles.Y = MathF.Abs(sinp) >= 1 ? MathF.CopySign(MathF.PI / 2, sinp) : // Use 90 degrees if gimbal lock
            MathF.Asin(sinp);

        // Yaw (rotation around Z-axis)
        var siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
        var cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
        angles.Z = MathF.Atan2(siny_cosp, cosy_cosp);

        return angles;
    }

    public static Quaternion LookRotation(Vector3 forward, Vector3? up = null)
    {
        up ??= Vector3.UnitY;
        var newForward = Vector3.Normalize(forward);
        var newRight = Vector3.Normalize(Vector3.Cross(up.Value, newForward));
        if (newRight == Vector3.NaN)
        {
            return FromToRotation(Vector3.UnitZ, newForward);
        }
        var newUp = Vector3.Normalize(Vector3.Cross(newForward, newRight));

        //fill matrix
        var mat = Matrix4x4.Identity;
        mat.M11 = newRight.X;
        mat.M21 = newRight.Y;
        mat.M31 = newRight.Z;
        mat.M41 = 0;
        mat.M12 = newUp.X;
        mat.M22 = newUp.Y;
        mat.M32 = newUp.Z;
        mat.M42 = 0;
        mat.M13 = newForward.X;
        mat.M23 = newForward.Y;
        mat.M33 = newForward.Z;
        mat.M43 = 0;

        //calc quaternion
        var quat = Quaternion.Identity;
        quat.W = MathF.Sqrt(1.0f + mat.M11 + mat.M22 + mat.M33) / 2.0f;
        var q4 = quat.W * 4;
        quat.X = (mat.M32 - mat.M23) / q4;
        quat.Y = (mat.M13 - mat.M31) / q4;
        quat.Z = (mat.M21 - mat.M12) / q4;

        return quat;
    }
    
    public static Quaternion FromToRotation(Vector3 fromDirection, Vector3 toDirection)
    {
        //calc rot angle
        var angleHalfRad =
            MathF.Acos(Vector3.Dot(fromDirection, toDirection) / (fromDirection.Length() * toDirection.Length())) /
            2.0f;

        //calc rot axis
        var axis = Vector3.Normalize(Vector3.Cross(fromDirection, toDirection));
        axis *= MathF.Sin(angleHalfRad);

        //calc quaternion
        var quat = new Quaternion(axis.X, axis.Y, axis.Z, MathF.Cos(angleHalfRad));

        return quat;
    }

    public static Quaternion ToQuaternion(this Direction2D dir) =>
        dir switch
        {
            Direction2D.Right => Quaternion.CreateFromAxisAngle(Vector3.UnitZ, float.DegreesToRadians(90f)),
            Direction2D.Left => Quaternion.CreateFromAxisAngle(Vector3.UnitZ, float.DegreesToRadians(-90f)),
            Direction2D.Front => Quaternion.CreateFromAxisAngle(Vector3.UnitX, float.DegreesToRadians(-90f)),
            Direction2D.Back => Quaternion.CreateFromAxisAngle(Vector3.UnitX, float.DegreesToRadians(90f)),
            _ => Quaternion.Identity
        };
}