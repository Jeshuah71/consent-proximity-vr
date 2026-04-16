using System;
using ConsentProximity.Core;
using UnityEngine;

namespace ConsentProximityFramework.Runtime.Proximity
{
    /// <summary>
    /// Throttled transform-based proximity checks with simple hysteresis to reduce range jitter.
    /// </summary>
    public class ProximityService : MonoBehaviour, IProximityService
    {
        [Header("Participants")]
        [SerializeField] private string participantAId = "A";
        [SerializeField] private string participantBId = "B";
        [SerializeField] private Transform participantA;
        [SerializeField] private Transform participantB;

        [Header("Thresholds")]
        [SerializeField] private float enterRangeMeters = 2f;
        [SerializeField] private float exitRangeMeters = 2.25f;
        [SerializeField] private float updatesPerSecond = 15f;

        public event Action<ParticipantId, ParticipantId, bool> OnRangeChanged;
        public event Action<ParticipantId, ParticipantId, float> OnDistanceUpdated;

        public float CurrentDistance { get; private set; } = float.MaxValue;

        private ParticipantId _participantAId;
        private ParticipantId _participantBId;
        private bool _isInRange;
        private float _nextUpdateTime;

        private void Awake()
        {
            _participantAId = new ParticipantId(participantAId);
            _participantBId = new ParticipantId(participantBId);
            exitRangeMeters = Mathf.Max(exitRangeMeters, enterRangeMeters);
        }

        private void Update()
        {
            if (participantA == null || participantB == null)
            {
                return;
            }

            if (Time.unscaledTime < _nextUpdateTime)
            {
                return;
            }

            _nextUpdateTime = Time.unscaledTime + (1f / Mathf.Max(1f, updatesPerSecond));
            EvaluateDistance();
        }

        public void Configure(ParticipantId aId, Transform a, ParticipantId bId, Transform b)
        {
            _participantAId = aId;
            _participantBId = bId;
            participantA = a;
            participantB = b;
        }

        public bool IsInRange(ParticipantId a, ParticipantId b)
        {
            return MatchesPair(a, b) && _isInRange;
        }

        public void EvaluateNow()
        {
            if (participantA == null || participantB == null)
            {
                CurrentDistance = float.MaxValue;
                return;
            }

            EvaluateDistance();
        }

        private void EvaluateDistance()
        {
            CurrentDistance = Vector3.Distance(participantA.position, participantB.position);
            OnDistanceUpdated?.Invoke(_participantAId, _participantBId, CurrentDistance);

            bool nextInRange = _isInRange
                ? CurrentDistance <= exitRangeMeters
                : CurrentDistance <= enterRangeMeters;

            if (nextInRange == _isInRange)
            {
                return;
            }

            _isInRange = nextInRange;
            OnRangeChanged?.Invoke(_participantAId, _participantBId, _isInRange);
        }

        private bool MatchesPair(ParticipantId a, ParticipantId b)
        {
            return (a == _participantAId && b == _participantBId) ||
                   (a == _participantBId && b == _participantAId);
        }
    }
}
