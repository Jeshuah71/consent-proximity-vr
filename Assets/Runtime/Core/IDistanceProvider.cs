namespace ConsentProximity.Core
{
    /// <summary>
    /// Abstraction for distance checks (meters) between two participants. Tests can fake this.
    /// XR/Netcode implements this.
    /// </summary>
    public interface IDistanceProvider
    {
        float GetDistanceMeters(ParticipantId a, ParticipantId b);
    }
}
