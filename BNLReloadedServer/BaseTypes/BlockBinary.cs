namespace BNLReloadedServer.BaseTypes;

public readonly ref struct BlockBinary(Span<byte> span, Vector3s pos)
{
    private readonly Span<byte> _span = span;

    public const int Size = 6;
    
    public ushort Id
    {
        get => BitConverter.ToUInt16(_span[..2]);
        set => BitConverter.TryWriteBytes(_span[..2], value);
    }

    public byte Damage
    {
        get => _span[2];
        set => _span[2] = value;
    }

    public ushort VData
    {
        get => BitConverter.ToUInt16(_span[3..5]);
        set => BitConverter.TryWriteBytes(_span[3..5], value);
    }

    public byte LData
    {
        get => _span[5];
        set => _span[5] = value;
    }
    
    public TeamType Team
    {
        get =>
            (LData & 3) switch
            {
                1 => TeamType.Team1,
                2 => TeamType.Team2,
                _ => TeamType.Neutral
            };
        set => LData = (byte) (LData & -4 | (!Card.HasTeam ? 0 : (int) value));
    }

    public Vector3s Position => pos;

    public short X => pos.x;
    public short Y => pos.y;
    public short Z => pos.z;
    
    public CardBlock Card => BlockCardsCache.GetCard(Id);
    
    public bool IsSolid => Card.Solid;

    public bool IsAir => Id == 0;

    public bool IsLocked => Id == 59;

    public bool IsReplaceable => Card.Replaceable || Card.IsVisualSlope && VData != 0;

    public bool IsPassable => Card.Passable != BlockPassableType.None || Card.IsVisualSlope && VData != 0;

    public bool IsGrounded => Card.Grounded;

    public bool IsTransparent => Card.Transparent;
    
    public bool CanFalling => Card.IsVisualGeneric ? Card.Visual?.Type != BlockVisualType.Highgrass && Card.Visual?.Type != BlockVisualType.Flatgrass : Card.IsVisualPrefab;

    public bool IsNoFallDamage => Card.Special is BlockSpecialNoFallDamage;

    public Block ToBlock() =>
        new()
        {
            Id = Id,
            Damage = Damage,
            Vdata = VData,
            Ldata = LData
        };
    
    public BlockUpdate ToUpdate(ushort? destAction = null) =>
        new()
        {
            Id = Id,
            Damage = Damage,
            Vdata = destAction ?? VData,
            Ldata = LData
        };

    public bool Equals(BlockBinary other) => other.Id == Id && other.Damage == Damage && other.VData == VData && other.LData == LData;
    
    public bool Equals(BlockUpdate update) => update.Id == Id && update.Damage == Damage && update.Vdata == VData && update.Ldata == LData;
}