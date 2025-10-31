namespace BNLReloadedServer.BaseTypes;

public abstract record EffectSource
{
    public ImpactData? Impact { get; protected init; }
    
    public virtual TeamType Team { get; }
}

public record UnitSource(Unit Unit) : EffectSource
{
    public UnitSource(Unit unit, ImpactData? impact) : this(unit)
    {
        Impact = impact;
    }

    public override TeamType Team => Unit.Team;

    public override int GetHashCode() => Unit.GetHashCode();

    public virtual bool Equals(UnitSource? other) => Unit.Equals(other?.Unit);
}

public record BlockSource(Vector3s Position, Block Block) : EffectSource
{
    public BlockSource(Vector3s position, Block block, ImpactData? impact) : this(position, block)
    {
        Impact = impact;
    }

    public override TeamType Team => Block.Team;

    public override int GetHashCode() => Position.GetHashCode();

    public virtual bool Equals(BlockSource? other) => Position.Equals(other?.Position);
}

public record TriggerSource(Unit Unit) : UnitSource(Unit)
{
    public TriggerSource(Unit unit, ImpactData? impact) : this(unit)
    {
        Impact = impact;
    }
}

public record PersistOnDeathSource(Unit Unit) : UnitSource(Unit)
{
    public PersistOnDeathSource(Unit unit, ImpactData? impact) : this(unit)
    {
        Impact = impact;
    }
}