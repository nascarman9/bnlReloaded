namespace BNLReloadedServer.BaseTypes;

public struct Glicko(float r, float d, float v)
{
    public float Rating = r;
    public float Deviation = d;
    public float Volatility = v;

    public override string ToString()
    {
        return $"{(object)Rating:0.0} RD:{(object)Deviation:0.0} σ:{(object)Volatility:0.00}";
    }
}