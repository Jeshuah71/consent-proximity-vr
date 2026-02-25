using UnityEngine;
using ConsentProximity.Core;
using ConsentProximity.StateMachine;

namespace ConsentProximity.TestHarness
{
    public class HarnessController : MonoBehaviour
    {
        [Header("Player Transforms")]
        public Transform playerA;
        public Transform playerB;

        [Header("Config")]
        public float maxRangeMeters = 2f;
        public float requestTimeoutSeconds = 8f;

        public ConsentStateMachine Machine { get; private set; }
        public float CurrentDistance { get; private set; }

        private TransformDistanceProvider _distanceProvider;
        private UnityClockAdapter _clock;

        private readonly ParticipantId _idA = new ParticipantId("A");
        private readonly ParticipantId _idB = new ParticipantId("B");

        void Awake()
        {
            _clock = gameObject.AddComponent<UnityClockAdapter>();

            _distanceProvider = new TransformDistanceProvider();
            _distanceProvider.Register(_idA, playerA);
            _distanceProvider.Register(_idB, playerB);

            var config = new ConsentConfig
            {
                maxRangeMeters = maxRangeMeters,
                requestTimeoutSeconds = requestTimeoutSeconds
            };

            Machine = new ConsentStateMachine(_idA, _idB, config, _clock, _distanceProvider);

            Machine.OnStateChanged += (prev, next) =>
                Debug.Log($"[Harness] State: {prev} → {next}");
            Machine.OnTerminated += reason =>
                Debug.Log($"[Harness] Terminated: {reason}");
        }

        void Update()
        {
            if (playerA == null || playerB == null) return;

            CurrentDistance = Vector3.Distance(playerA.position, playerB.position);
            Machine.SetInRange(CurrentDistance <= maxRangeMeters);
            Machine.Tick();

            if (Input.GetKeyDown(KeyCode.R)) Machine.RequestConsent(_idB); // B requests
            if (Input.GetKeyDown(KeyCode.A)) Machine.Accept(_idA);         // A accepts
            if (Input.GetKeyDown(KeyCode.W)) Machine.Withdraw(_idB);       // B withdraws
            if (Input.GetKeyDown(KeyCode.C)) Machine.Cancel(_idB);         // B cancels
            if (Input.GetKeyDown(KeyCode.X)) Machine.Withdraw(_idA);       // A rejects/blocks B
        }
    }
}