using ConsentProximity.Core;
using ConsentProximity.StateMachine;
using ConsentProximityFramework.Runtime.Proximity;
using UnityEngine;

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

        [Header("Services")]
        [SerializeField] private ProximityService proximityService;

        public ConsentStateMachine Machine { get; private set; }
        public float CurrentDistance { get; private set; }

        private TransformDistanceProvider _distanceProvider;
        private UnityClockAdapter _clock;

        private readonly ParticipantId _idA = new ParticipantId("A");
        private readonly ParticipantId _idB = new ParticipantId("B");

        private void Awake()
        {
            _clock = gameObject.AddComponent<UnityClockAdapter>();

            if (proximityService == null)
            {
                proximityService = gameObject.AddComponent<ProximityService>();
            }

            _distanceProvider = new TransformDistanceProvider();
            _distanceProvider.Register(_idA, playerA);
            _distanceProvider.Register(_idB, playerB);
            proximityService.Configure(_idA, playerA, _idB, playerB);

            var config = new ConsentConfig
            {
                maxRangeMeters = maxRangeMeters,
                requestTimeoutSeconds = requestTimeoutSeconds
            };

            Machine = new ConsentStateMachine(_idA, _idB, config, _clock, _distanceProvider);

            Machine.OnStateChanged += (prev, next) =>
                Debug.Log($"[Harness] State: {prev} -> {next}");
            Machine.OnTerminated += reason =>
                Debug.Log($"[Harness] Terminated: {reason}");

            proximityService.OnRangeChanged += (_, _, isInRange) => Machine.SetInRange(isInRange);
            proximityService.OnDistanceUpdated += (_, _, distanceMeters) => CurrentDistance = distanceMeters;
        }

        private void Update()
        {
            if (playerA == null || playerB == null)
            {
                return;
            }

            proximityService.EvaluateNow();
            Machine.Tick();

            if (Input.GetKeyDown(KeyCode.R)) Machine.RequestConsent(_idB);
            if (Input.GetKeyDown(KeyCode.A)) Machine.Accept(_idA);
            if (Input.GetKeyDown(KeyCode.W)) Machine.Withdraw(_idB);
            if (Input.GetKeyDown(KeyCode.C)) Machine.Cancel(_idB);
            if (Input.GetKeyDown(KeyCode.X)) Machine.Withdraw(_idA);
        }
    }
}
