namespace BNLReloadedServer.BaseTypes;

public enum StableDirection : ushort
{
    Above, // 0,1,0
    Below, // 0,-1,0
    Right, // 1,0,0
    Left,  // -1,0,0
    Forward, // 0,0,1
    Back,  // 0,0,-1
    Inherent
}

public static class StableHelper
{
    public static Vector3s ToVector(this StableDirection direction) =>
        direction switch
        {
            StableDirection.Above => Vector3s.Up,
            StableDirection.Below => Vector3s.Down,
            StableDirection.Right => Vector3s.Right,
            StableDirection.Left => Vector3s.Left,
            StableDirection.Forward => Vector3s.Forward,
            StableDirection.Back => Vector3s.Back,
            _ => Vector3s.Zero
        };

    public static StableDirection ToStableDirection(this Vector3s stable, Vector3s attached) =>
        attached.x > stable.x ? StableDirection.Right :
        attached.x < stable.x ? StableDirection.Left :
        attached.y > stable.y ? StableDirection.Above :
        attached.y < stable.y ? StableDirection.Below :
        attached.z > stable.z ? StableDirection.Forward :
        attached.z < stable.z ? StableDirection.Back : StableDirection.Inherent;
}