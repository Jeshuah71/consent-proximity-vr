using UnityEngine;
using System;

namespace ConsentProximity.Core
{
    [Serializable]
    public sealed class ConsentConfig
    {
        public float maxRangeMeters = 2.0f;
        public float requestTimeoutSeconds = 8.0f;
    }
}
