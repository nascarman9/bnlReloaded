using System.Numerics;
using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.ProtocolHelpers;

public static class PrefabBuilder
{
  private static readonly BlockFace[,,] PrefabFaces = new BlockFace[4, 6, 6];

  static PrefabBuilder() => GenerateFaceTable();
  
  public static byte MakeData(BlockVisual info, BlockFace face, int rotationIndex, int prefabIndex)
  {
    var num = 0;
    if (info.FaceAlign ?? false)
      num = (int) ((BlockFace) num | face & (BlockFace.Left | BlockFace.Forward));
    if (info.Rotation ?? false)
      num |= ((int)MathF.Abs(rotationIndex) % 4 & 3) << 3;
    
    if (info.Prefabs is not { Count: > 0 })
    {
      return (byte)num;
    }
    
    return (byte) (num | (prefabIndex % info.Prefabs.Count & 7) << 5);
  }

  public static bool IsSolidFace(BlockBinary block, BlockFace dir)
  {
    if (block.Card.Visual is not {Type: BlockVisualType.Prefab, Prefabs: not null} visual)
      return false;
    
    var dir1 = TransformFace(dir, visual, (byte) block.VData);
    return GetPrefabFaceFlag(visual.Prefabs[GetPrefabIndex((byte) block.VData)], dir1);
  }

  private static BlockFace TransformFace(BlockFace dir, BlockVisual info, byte data)
  {
    var index1 = 0;
    if (info.Rotation ?? false)
      index1 = GetRotationIndex(data);
    var index2 = BlockFace.Bottom;
    if (info.FaceAlign ?? false)
      index2 = GetFace(data);
    return PrefabFaces[index1, (int) index2, (int) dir];
  }

  private static bool GetPrefabFaceFlag(BlockPrefab prefab, BlockFace dir) =>
    dir switch
    {
      BlockFace.Top => prefab.IsTop,
      BlockFace.Bottom => prefab.IsBottom,
      BlockFace.Right => prefab.IsRight,
      BlockFace.Left => prefab.IsLeft,
      BlockFace.Forward => prefab.IsForward,
      BlockFace.Back => prefab.IsBack,
      _ => false
    };

  private static BlockMirrorType GetMirrorType(BlockVisual info, byte data)
  {
    return info.Prefabs?[GetPrefabIndex(data)].Mirror ?? BlockMirrorType.Square;
  }

  private static BlockFace GetFace(byte data) => (BlockFace) (data & 7U);

  private static int GetRotationIndex(byte data) => data >> 3 & 3;

  public static int GetPrefabIndex(byte data) => data >> 5 & 7;

  private static float GetRotation(byte data) => 90f * GetRotationIndex(data);

  public static Quaternion FaceToRotation(BlockFace face)
  {
    return face switch
    {
      BlockFace.Top => Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI),
      BlockFace.Right => Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI * 0.5f),
      BlockFace.Left => Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -MathF.PI * 0.5f),
      BlockFace.Forward => Quaternion.CreateFromAxisAngle(Vector3.UnitX, -MathF.PI * 0.5f),
      BlockFace.Back => Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI * 0.5f),
      _ => Quaternion.Identity
    };
  }

  private static void GenerateFaceTable()
  {
    for (var index1 = 0; index1 < 4; ++index1)
    {
      for (var face1 = 0; face1 < 6; ++face1)
      {
        for (var index2 = 0; index2 < 6; ++index2)
        {
          var face2 = CoordsHelper.VectorToFace((Vector3s)CoordsHelper.Round(
            Vector3.Transform(CoordsHelper.FaceToNormal[index2],
              Quaternion.Inverse(Quaternion.CreateFromAxisAngle(Vector3.UnitY, float.DegreesToRadians(index1 * 90))) *
            Quaternion.Inverse(FaceToRotation((BlockFace)face1)))));
          PrefabFaces[index1, face1, index2] = face2;
        }
      }
    }
  }
}