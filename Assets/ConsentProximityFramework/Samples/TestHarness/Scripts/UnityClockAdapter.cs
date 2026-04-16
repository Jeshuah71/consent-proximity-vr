using UnityEngine;
using ConsentProximity.Core;

namespace ConsentProximity.TestHarness
{
    public sealed class UnityClockAdapter : MonoBehaviour, IClock
    {
        public float Now => Time.time;
    }
}