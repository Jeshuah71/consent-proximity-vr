using UnityEngine;
using ConsentProximity.Core;

namespace ConsentProximity.TestHarness
{
    public sealed class TransformDistanceProvider : IDistanceProvider
    {
        private Transform _a;
        private Transform _b;

        public void Register(ParticipantId id, Transform t)
        {
            if (id == new ParticipantId("A")) _a = t;
            else if (id == new ParticipantId("B")) _b = t;
        }

        public float GetDistanceMeters(ParticipantId a, ParticipantId b)
        {
            if (_a == null || _b == null) return float.MaxValue;
            return Vector3.Distance(_a.position, _b.position);
        }
    }
}