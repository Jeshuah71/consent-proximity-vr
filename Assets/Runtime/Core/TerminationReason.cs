namespace ConsentProximity.Core
{
    public enum TerminationReason
    {
        Unknown = 0,
        WithdrawnConsent = 1,
        Timeout = 2,
        Cancelled = 3,
        DistanceExceeded = 4,
        DuplicateRequest = 5,
        InvalidAction = 6
    }
}
