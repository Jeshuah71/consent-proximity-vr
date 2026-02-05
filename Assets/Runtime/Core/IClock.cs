using UnityEngine;

namespace ConsentProximity.Core
{
    /// <summary>
    /// Abstraction for time (seconds). Lets tests control time precisely.
    /// </summary>
    public interface IClock
    {
        float Now { get; }
    }
}
