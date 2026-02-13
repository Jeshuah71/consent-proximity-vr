using ConsentProximity.Core;

public sealed class FakeDistanceProvider : IDistanceProvider
{
    public float DistanceMeters { get; set; }

    public float GetDistanceMeters(ParticipantId a, ParticipantId b)
    {
        return DistanceMeters;
    }
}
