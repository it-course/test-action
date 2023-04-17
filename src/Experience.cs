using devrating.factory;

public sealed class Experience
{
    private readonly Formula _formula;

    public Experience(Formula formula)
    {
        _formula = formula;
    }

    public uint Growth(double rating)
    {
        return (uint)(_formula.WinProbabilityOfA(rating, _formula.DefaultRating()) * 100d);
    }
}