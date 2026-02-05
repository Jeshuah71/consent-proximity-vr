namespace ConsentProximityFramework.Core
{
    public enum InteractionState
    {
        Idle,       // No one nearby
        InRange,    // (If we use lines/boundaries) Detected at Red Line (Permission)
        Pending,    // (If we use lines/boundaries) At Orange Gate (Waiting)
        Active,     // Approved
        Terminated  // Ended
    }
}