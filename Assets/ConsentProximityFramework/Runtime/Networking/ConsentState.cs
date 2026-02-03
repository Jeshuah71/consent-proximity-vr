namespace ConsentProximityFramework.Runtime.Networking
{
    ///Summary>
    /// Reprensent the current consent state between two users
    /// only state can be at a time
    /// </summary>
    public enum ConsentState
    {
        Idle, //no iteration, default state
        InRange, // users are close enough to interac
        Requested, // Consent request has be sent
        Active, // Mutual consent granted
        Terminated //interaction ended or cancelled
    }
}