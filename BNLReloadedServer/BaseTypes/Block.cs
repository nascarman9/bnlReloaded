namespace BNLReloadedServer.BaseTypes;

public struct Block : IEquatable<Block>
{
  public const int MaxBlockDamage = 255;
  public const int NoDamageTextureIndex = 255;
  public ushort Id;
  public byte Damage;
  public ushort Vdata;
  public byte Ldata;
  public ushort Light;
  public byte Color;

  public Block(ushort id = 0)
  {
    Id = id;
    Light = 0;
    Vdata = 0;
    Ldata = 0;
    Damage = 0;
    Color = 0;
  }

  public Block(BlockUpdate update)
  {
    Id = update.Id;
    Damage = update.Damage;
    Vdata = update.Vdata;
    Ldata = update.Ldata;
    Light = 0;
    Color = 0;
  }

  public CardBlock Card => BlockCardsCache.GetCard(Id);

  public BlockUpdate ToUpdate()
  {
    return new BlockUpdate
    {
      Id = Id,
      Damage = Damage,
      Vdata = Vdata,
      Ldata = Ldata
    };
  }

  public bool Compare(BlockUpdate bu) => Id == bu.Id && Damage == bu.Damage && Vdata == bu.Vdata && Ldata == bu.Ldata;

  public static bool CompareIdData(Block b1, Block b2) => b1.Id == b2.Id && b1.Vdata == b2.Vdata && b1.Ldata == b2.Ldata;

  public static bool CompareWithoutLight(Block b1, Block b2)
  {
    return b1.Id == b2.Id && b1.Damage == b2.Damage && b1.Vdata == b2.Vdata && b1.Ldata == b2.Ldata && b1.Color == b2.Color;
  }

  public override bool Equals(object? obj) => obj is Block block && this == block;

  public override int GetHashCode()
  {
    return (((17 * 23 + Id.GetHashCode()) * 29 + Damage.GetHashCode()) * 31 + Vdata.GetHashCode()) * 33 + Ldata.GetHashCode();
  }

  public bool IsSolid => Card.Solid;

  public bool IsLocked => Id == 59;

  public bool IsReplaceable => Card.Replaceable || Card.IsVisualSlope && Vdata != 0;

  public bool IsPassable => Card.Passable != BlockPassableType.None || Card.IsVisualSlope && Vdata != 0;

  public bool IsGrounded => Card.Grounded;

  public bool IsTransparent => Card.Transparent;

  public TeamType Team
  {
    get
    {
      return (Ldata & 3) switch
      {
        1 => TeamType.Team1,
        2 => TeamType.Team2,
        _ => TeamType.Neutral
      };
    }
    set => Ldata = (byte) (Ldata & -4 | (!Card.HasTeam ? 0 : (int) value));
  }

  public int DamageIndex(int count)
  {
    return Convert.ToInt32(Damage / (float) byte.MaxValue * (count - 1));
  }

  public int DamageTextureIndex()
  {
    var card = Card;
    var num = Damage / (float) byte.MaxValue;
    if (card.Visual?.DecalIndices == null || num <= 0.0)
      return byte.MaxValue;
    var index = Convert.ToInt32(num * (card.Visual.DecalIndices.Count - 1));
    return card.Visual.DecalIndices[index];
  }

  public int? DamageTextureIndexInRange()
  {
    var card = Card;
    var index = Convert.ToInt32(Damage / (float) byte.MaxValue * (card.Visual?.DecalIndices?.Count - 1));
    return card.Visual?.DecalIndices?[index];
  }

  public bool CanFalling => Card.IsVisualGeneric ? Card.Visual?.Type != BlockVisualType.Highgrass && Card.Visual?.Type != BlockVisualType.Flatgrass : Card.IsVisualPrefab;

  public bool IsSlope => Card.IsVisualSlope;

  public bool IsNoFallDamage => Card.Special is BlockSpecialNoFallDamage;

  public bool IsLightTransparent => Card.IsVisualSlope ? Vdata != 0 : Card.LightTransparent;

  public bool IsSkyTransparent => Card.IsVisualSlope ? Vdata != 0 : Card.SkylightTransparent;

  public byte LightFade => Card.LightFade;

  public byte SelfLight => Card.SelfLight;

  public byte GetLight(LightLayer layer)
  {
    return layer switch
    {
      LightLayer.Sky => (byte)((uint)Light >> 12),
      LightLayer.Block => (byte)((Light & 3840) >> 8),
      _ => 0
    };
  }

  public byte GetLight()
  {
    return (byte) Math.Max(GetLight(LightLayer.Sky), (int) GetLight(LightLayer.Block));
  }

  public byte GetLight(out LightLayer layer)
  {
    var light1 = GetLight(LightLayer.Sky);
    var light2 = GetLight(LightLayer.Block);
    if (light1 > light2)
    {
      layer = LightLayer.Sky;
      return light1;
    }
    layer = LightLayer.Block;
    return light2;
  }

  public void SetLight(byte lightValue, LightLayer layer)
  {
    Light = layer switch
    {
      LightLayer.Sky => (ushort)(Light & 4095 | (ushort)(lightValue & 15U) << 12),
      LightLayer.Block => (ushort)(Light & 61695 | (ushort)(lightValue & 15U) << 8),
      _ => Light
    };
  }

  public byte GetBlockLightVariant() => (byte) ((Light & 240) >> 4);

  public void SetBlockLightVariant(byte variant)
  {
    Light = (ushort) (Light & 65295 | (ushort) (variant & 15U) << 8);
  }

  public static bool operator ==(Block b1, Block b2)
  {
    return b1.Id == b2.Id && b1.Damage == b2.Damage && b1.Vdata == b2.Vdata && b1.Ldata == b2.Ldata;
  }

  public static bool operator !=(Block b1, Block b2)
  {
    return b1.Id != b2.Id || b1.Damage != b2.Damage || b1.Vdata != b2.Vdata || b1.Ldata != b2.Ldata;
  }

  public bool Equals(Block other)
  {
    return Id == other.Id && Damage == other.Damage && Vdata == other.Vdata && Ldata == other.Ldata && Light == other.Light && Color == other.Color;
  }
}