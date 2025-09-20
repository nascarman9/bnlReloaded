using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.ProtocolHelpers;

public static class SlopeBuilder
{
  public static readonly BlockCorner[][] SidesCorners = new BlockCorner[][]
  {
    [
      BlockCorner.C011,
      BlockCorner.C010,
      BlockCorner.C110,
      BlockCorner.C111
    ],
    [
      BlockCorner.C101,
      BlockCorner.C100,
      BlockCorner.C000,
      BlockCorner.C001
    ],
    [
      BlockCorner.C101,
      BlockCorner.C111,
      BlockCorner.C110,
      BlockCorner.C100
    ],
    [
      BlockCorner.C000,
      BlockCorner.C010,
      BlockCorner.C011,
      BlockCorner.C001
    ],
    [
      BlockCorner.C001,
      BlockCorner.C011,
      BlockCorner.C111,
      BlockCorner.C101
    ],
    [
      BlockCorner.C100,
      BlockCorner.C110,
      BlockCorner.C010,
      BlockCorner.C000
    ]
  };
  public static readonly BlockCorner[][] AdjacentCorners = new BlockCorner[][]
  {
    [
      BlockCorner.C100,
      BlockCorner.C010,
      BlockCorner.C001
    ],
    [
      BlockCorner.C110,
      BlockCorner.C000,
      BlockCorner.C101
    ],
    [
      BlockCorner.C110,
      BlockCorner.C011,
      BlockCorner.C000
    ],
    [
      BlockCorner.C000,
      BlockCorner.C011,
      BlockCorner.C101
    ],
    [
      BlockCorner.C111,
      BlockCorner.C010,
      BlockCorner.C100
    ],
    [
      BlockCorner.C001,
      BlockCorner.C010,
      BlockCorner.C111
    ],
    [
      BlockCorner.C111,
      BlockCorner.C100,
      BlockCorner.C001
    ],
    [
      BlockCorner.C101,
      BlockCorner.C011,
      BlockCorner.C110
    ]
  };
  public static readonly BlockCorner[][] OppositeAxisCorner = new BlockCorner[][]
  {
    [
      BlockCorner.C100,
      BlockCorner.C000,
      BlockCorner.C110,
      BlockCorner.C101,
      BlockCorner.C010,
      BlockCorner.C111,
      BlockCorner.C001,
      BlockCorner.C011
    ],
    [
      BlockCorner.C010,
      BlockCorner.C110,
      BlockCorner.C000,
      BlockCorner.C011,
      BlockCorner.C100,
      BlockCorner.C001,
      BlockCorner.C111,
      BlockCorner.C101
    ],
    [
      BlockCorner.C001,
      BlockCorner.C101,
      BlockCorner.C011,
      BlockCorner.C000,
      BlockCorner.C111,
      BlockCorner.C010,
      BlockCorner.C100,
      BlockCorner.C110
    ]
  };
  public static readonly BlockCorner[] RotateCWCorner =
  [
    BlockCorner.C001,
    BlockCorner.C000,
    BlockCorner.C011,
    BlockCorner.C101,
    BlockCorner.C010,
    BlockCorner.C111,
    BlockCorner.C100,
    BlockCorner.C110
  ];

  public static byte MakeSlopeData(HashSet<BlockCorner> existent)
  {
    var maxValue = existent.Aggregate<BlockCorner, int>(byte.MaxValue,
      (current, blockCorner) => current & ~(1 << (int)(blockCorner & (BlockCorner)31 & (BlockCorner)31)));
    return (byte) maxValue;
  }

  public static bool IsCorner(BlockCorner corner, byte data) => (data & 1 << (int) (corner & (BlockCorner) 31)) == 0;

  public static HashSet<BlockCorner> ExistentCorners(byte data)
  {
    HashSet<BlockCorner> blockCornerSet = [];
    foreach (var allCorner in CoordsHelper.AllCorners)
    {
      if (IsCorner(allCorner, data))
        blockCornerSet.Add(allCorner);
    }
    return blockCornerSet;
  }

  public static HashSet<BlockCorner> MissedCorners(byte data)
  {
    HashSet<BlockCorner> blockCornerSet = [];
    foreach (var allCorner in CoordsHelper.AllCorners)
    {
      if (!IsCorner(allCorner, data))
        blockCornerSet.Add(allCorner);
    }
    return blockCornerSet;
  }
}