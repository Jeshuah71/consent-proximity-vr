namespace ConsentProximityFramework.Runtime.Networking
{
    /// <summary>
    /// Represents the current consent state between two users.
    /// Only one state can be active at a time.
    /// </summary>
    public enum ConsentState
    {
        Idle,
        InRange,
        Requested,
        Active,
        Terminated
    }
}