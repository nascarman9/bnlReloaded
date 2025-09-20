using System.Numerics;
using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.ProtocolHelpers;

public static class BuildHelper
{
    public enum BluidAttachmentType
    {
        Floor,
        Ceiling,
        Walls,
    }
    
    public static BluidAttachmentType GetAttachmentType(
        Vector3s inBlockPos,
        Vector3s onBlockPos)
    {
        var num = onBlockPos.y - inBlockPos.y;
        if (num > 0)
            return BluidAttachmentType.Ceiling;
        return num < 0 ? BluidAttachmentType.Floor : BluidAttachmentType.Walls;
    }

    public static Quaternion GetBuildRotation(CardDevice deviceCard, Vector3s blockPos, Vector3s blockPosOn, Direction2D direction) =>
        GetAttachmentType(blockPos, blockPosOn) switch
        {
            BluidAttachmentType.Floor when deviceCard.AttachFloor => Direction2dHelper.Rotation(direction),
            
            BluidAttachmentType.Floor when deviceCard.AttachCeiling => Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI),
            
            BluidAttachmentType.Floor => Direction2dHelper.Rotation(direction),
            
            BluidAttachmentType.Ceiling when deviceCard.AttachCeiling => Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI),
            
            BluidAttachmentType.Ceiling => Direction2dHelper.Rotation(direction),
            
            BluidAttachmentType.Walls when deviceCard.AttachWalls => Direction2dHelper.RotationForWall(
                                                                         Direction2dHelper.FromVector3s(blockPosOn -
                                                                             blockPos) ?? direction) * 
                                                                     PrefabBuilder.FaceToRotation(
                                                                         CoordsHelper.VectorToFace(blockPosOn - blockPos)),
            
            BluidAttachmentType.Walls when deviceCard.AttachFloor => Direction2dHelper.Rotation(direction),
            
            BluidAttachmentType.Walls when deviceCard.AttachCeiling => Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI),
            
            _ => Quaternion.Identity
        };
}