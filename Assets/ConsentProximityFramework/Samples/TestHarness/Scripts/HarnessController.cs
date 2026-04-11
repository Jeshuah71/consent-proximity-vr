using System;
using ConsentProximity.Core;
using ConsentProximity.StateMachine;
using ConsentProximityFramework.Runtime.ConsentUI;
using ConsentProximityFramework.Runtime.Feedback;
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

        [Header("UI & Feedback (optional — auto-found if empty)")]
        [SerializeField] private ConsentUIPanel consentUI;
        [SerializeField] private FeedbackManager feedbackManager;

        public ConsentStateMachine Machine { get; private set; }
        public float CurrentDistance { get; private set; }

        /// <summary>Fired whenever the consent state changes. Other components can subscribe.</summary>
        public event Action<ConsentState> OnStateChanged;

        private TransformDistanceProvider _distanceProvider;
        private UnityClockAdapter _clock;

        private readonly ParticipantId _idA = new ParticipantId("A");
        private readonly ParticipantId _idB = new ParticipantId("B");

        private void Awake()
        {
            _clock = gameObject.AddComponent<UnityClockAdapter>();

            if (proximityService == null)
                proximityService = gameObject.AddComponent<ProximityService>();
            if (consentUI == null)
                consentUI = FindFirstObjectByType<ConsentUIPanel>();
            if (feedbackManager == null)
                feedbackManager = FindFirstObjectByType<FeedbackManager>();

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
            {
                Debug.Log($"[Harness] State: {prev} -> {next}");
                HandleStateChanged(next);
            };
            Machine.OnTerminated += reason =>
                Debug.Log($"[Harness] Terminated: {reason}");

            proximityService.OnRangeChanged += (_, _, isInRange) => Machine.SetInRange(isInRange);
            proximityService.OnDistanceUpdated += (_, _, distanceMeters) => CurrentDistance = distanceMeters;

            // Wire ConsentUIPanel button events back into the state machine
            if (consentUI != null)
            {
                WireConsentUI();
            }
        }

        private void HandleStateChanged(ConsentState state)
        {
            OnStateChanged?.Invoke(state);

            // Drive the UI panel based on state
            if (consentUI != null)
            {
                switch (state)
                {
                    case ConsentState.Requested:
                        consentUI.ShowRequest();
                        break;
                    case ConsentState.Active:
                        consentUI.ShowWithdrawOnly();
                        break;
                    default:
                        consentUI.HideAll();
                        break;
                }
            }

            // Drive the feedback manager (if not already wired via ConsentFlowManager)
            if (feedbackManager != null && feedbackManager.flowManager == null)
            {
                feedbackManager.HandleStateChanged(state);
            }
        }

        private void WireConsentUI()
        {
            // When the user clicks Accept on the UI panel → accept consent
            consentUI.gameObject.AddComponent<ConsentUIBridge>().Init(
                onAccept: () => Machine.Accept(_idA),
                onReject: () => Machine.Cancel(_idB),
                onWithdraw: () => Machine.Withdraw(_idA)
            );
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

    /// <summary>
    /// Tiny bridge that connects ConsentUIPanel UnityEvents to state machine actions.
    /// Added at runtime by HarnessController so no Inspector wiring is needed.
    /// </summary>
    public class ConsentUIBridge : MonoBehaviour
    {
        private Action _onAccept, _onReject, _onWithdraw;

        public void Init(Action onAccept, Action onReject, Action onWithdraw)
        {
            _onAccept = onAccept;
            _onReject = onReject;
            _onWithdraw = onWithdraw;
        }

        // Called by ConsentUIPanel UnityEvent buttons
        public void OnAccept() => _onAccept?.Invoke();
        public void OnReject() => _onReject?.Invoke();
        public void OnWithdraw() => _onWithdraw?.Invoke();
    }
}
