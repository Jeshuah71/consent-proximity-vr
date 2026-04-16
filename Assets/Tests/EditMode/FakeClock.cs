using ConsentProximity.Core;

public sealed class FakeClock : IClock
{
    public float Now { get; private set; }

    public void Advance(float seconds)
    {
        Now += seconds;
    }

    public void Set(float now)
    {
        Now = now;
    }
}
