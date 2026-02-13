using System;

namespace ConsentProximity.Core
{
    /// <summary>
    /// Template abstraction for proximity checks and notifications.
    /// </summary>
    public interface IProximityService
    {
        bool IsInRange(ParticipantId a, ParticipantId b);

        event Action<ParticipantId, ParticipantId, bool> OnRangeChanged;
    }
}
