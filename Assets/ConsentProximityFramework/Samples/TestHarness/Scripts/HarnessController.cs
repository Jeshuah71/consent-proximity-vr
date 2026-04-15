using System;
using System.Collections;
using ConsentProximity.Core;
using ConsentProximity.StateMachine;
using ConsentProximityFramework.Runtime.Feedback;
using ConsentProximityFramework.Runtime.Proximity;
using ConsentProximityFramework.Runtime.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ConsentProximity.TestHarness
{
    public class HarnessController : MonoBehaviour
    {
        [Header("Player Transforms")]
        [Tooltip("Player A's HEAD position (used for distance checks). Drag the Main Camera / CenterEyeAnchor here for VR.")]
        public Transform playerA;

        [Tooltip("Optional — the rig root to move for push-back. If empty, uses playerA. For VR, set this to the Camera Rig root.")]
        public Transform playerARigRoot;

        public Transform playerB;

        [Header("Config")]
        [Tooltip("Distance at which the consent popup is automatically triggered.")]
        public float maxRangeMeters = 2f;
        public float requestTimeoutSeconds = 60f;

        [Header("Protected Zone")]
        [Tooltip("Player cannot get closer than this unless consent is Active. Soft-pushes them back.")]
        public float minSafeDistanceMeters = 0.8f;

        [Tooltip("Automatically request consent when Player A enters range (no need to press R).")]
        public bool autoRequestOnInRange = true;

        [Header("Session Rules")]
        [Tooltip("After a rejection, how long Player A must wait before consent can be requested again.")]
        public float rejectionCooldownSeconds = 60f;

        [Tooltip("Once Player B has accepted once, never auto-request again for the rest of this session.")]
        public bool oneConsentPerSession = true;

        [Header("Services")]
        [SerializeField] private ProximityService proximityService;

        [Header("UI & Feedback (optional — auto-found if empty)")]
        [SerializeField] private ConsentUI consentUI;
        [SerializeField] private FeedbackManager feedbackManager;

        public ConsentStateMachine Machine { get; private set; }
        public float CurrentDistance { get; private set; }

        /// <summary>Fired whenever the consent state changes. Other components can subscribe.</summary>
        public event Action<ConsentState> OnStateChanged;

        private TransformDistanceProvider _distanceProvider;
        private UnityClockAdapter _clock;

        private readonly ParticipantId _idA = new ParticipantId("A");
        private readonly ParticipantId _idB = new ParticipantId("B");

        // Session tracking
        private bool _isInRange;
        private bool _sessionConsented;   // True once Active has been reached at least once
        private float _cooldownEndTime;   // Time.time at which the rejection cooldown ends

        private void Awake()
        {
            _clock = gameObject.AddComponent<UnityClockAdapter>();

            if (proximityService == null)
                proximityService = gameObject.AddComponent<ProximityService>();
            if (consentUI == null)
                consentUI = FindFirstObjectByType<ConsentUI>();
            if (feedbackManager == null)
                feedbackManager = FindFirstObjectByType<FeedbackManager>();

            _distanceProvider = new TransformDistanceProvider();
            _distanceProvider.Register(_idA, playerA);
            _distanceProvider.Register(_idB, playerB);
            proximityService.Configure(_idA, playerA, _idB, playerB);

            BuildMachine();

            proximityService.OnRangeChanged += HandleRangeChanged;
            proximityService.OnDistanceUpdated += (_, _, distanceMeters) => CurrentDistance = distanceMeters;

            // Wire ConsentUIPanel button events back into the state machine
            if (consentUI != null)
            {
                WireConsentUI();
            }
        }

        /// <summary>
        /// Creates a fresh ConsentStateMachine. Called at start and whenever we need
        /// to reset after a Terminated state (so the next range-entry triggers a new flow).
        /// </summary>
        private void BuildMachine()
        {
            if (Machine != null)
            {
                Machine.OnStateChanged -= HandleMachineStateChanged;
            }

            var config = new ConsentConfig
            {
                maxRangeMeters = maxRangeMeters,
                requestTimeoutSeconds = requestTimeoutSeconds
            };

            Machine = new ConsentStateMachine(_idA, _idB, config, _clock, _distanceProvider);
            Machine.OnStateChanged += HandleMachineStateChanged;
            Machine.OnTerminated += reason =>
                Debug.Log($"[Harness] Terminated: {reason}");
        }

        private void HandleMachineStateChanged(ConsentState prev, ConsentState next)
        {
            Debug.Log($"[Harness] State: {prev} -> {next}");

            // Track "session consented" flag — once we've hit Active, the session is done
            if (next == ConsentState.Active)
            {
                _sessionConsented = true;
                FlashConsentPanel(new Color(0.2f, 0.8f, 0.3f, 0.9f), 1.0f); // green
            }

            // Rejection / timeout from Requested → flash red before hide
            if (next == ConsentState.Terminated && prev == ConsentState.Requested)
            {
                FlashConsentPanel(new Color(0.85f, 0.25f, 0.25f, 0.9f), 1.0f); // red
            }

            // When we hit Terminated from Requested by Cancel/Timeout → start rejection cooldown
            if (next == ConsentState.Terminated && prev == ConsentState.Requested)
            {
                var reason = Machine.LastTermination;
                if (reason == TerminationReason.Cancelled || reason == TerminationReason.Timeout)
                {
                    _cooldownEndTime = Time.time + rejectionCooldownSeconds;
                    Debug.Log($"[Harness] Rejection cooldown active for {rejectionCooldownSeconds}s");
                }
            }

            HandleStateChanged(next);
        }

        private void HandleRangeChanged(ParticipantId _, ParticipantId __, bool isInRange)
        {
            _isInRange = isInRange;

            // Rebuild the machine on range ENTRY if the previous interaction ended in Terminated.
            // This covers the common case where Player A walks away during Requested (which
            // auto-terminates via DistanceExceeded) and then returns — without this, the machine
            // stays stuck in Terminated forever and the popup never fires again.
            if (isInRange && Machine.State == ConsentState.Terminated)
            {
                BuildMachine();
            }

            Machine.SetInRange(isInRange);
        }

        private void HandleStateChanged(ConsentState state)
        {
            OnStateChanged?.Invoke(state);

            // Auto-request consent as soon as Player A enters range — unless gated
            if (state == ConsentState.InRange && autoRequestOnInRange)
            {
                if (_sessionConsented && oneConsentPerSession)
                {
                    Debug.Log("[Harness] Session already consented; not auto-requesting.");
                }
                else if (Time.time < _cooldownEndTime)
                {
                    float remaining = _cooldownEndTime - Time.time;
                    Debug.Log($"[Harness] Cooldown active ({remaining:F1}s remaining); not auto-requesting.");
                }
                else
                {
                    Machine.RequestConsent(_idA);
                }
            }

            // Drive the UI panel based on the CURRENT machine state — not the `state` parameter,
            // which can be stale if auto-request above caused a recursive transition (e.g.
            // InRange → Requested) that already updated the UI. Using Machine.State prevents
            // the outer InRange call from hitting the `default` branch and hiding the popup
            // that the recursive Requested call just showed.
            var currentState = Machine != null ? Machine.State : state;

            // If a wall-button decision was queued, fire it now that we're in the request zone.
            if (currentState == ConsentState.Requested)
            {
                TryFireQueuedResponse();
            }

            if (consentUI != null)
            {
                switch (currentState)
                {
                    case ConsentState.Requested:
                        consentUI.Show();
                        break;
                    case ConsentState.Active:
                        consentUI.ShowWithdrawOnly();
                        break;
                    default:
                        consentUI.Hide();
                        break;
                }
            }

            // Drive the feedback manager (if not already wired via ConsentFlowManager)
            if (feedbackManager != null && feedbackManager.flowManager == null)
            {
                feedbackManager.HandleStateChanged(state);
            }
        }

        /// <summary>
        /// If Player A (the VR player) crosses the protected zone without consent,
        /// softly push them back outside it. Prevents getting closer than minSafeDistance
        /// until the interaction is Active.
        /// </summary>
        private void EnforceProtectedZone()
        {
            if (playerA == null || playerB == null) return;
            if (Machine == null) return;
            if (Machine.State == ConsentState.Active) return; // Consent granted — free movement
            if (_sessionConsented && oneConsentPerSession) return; // Once accepted, consent persists for the session (reset clears this)

            // Flatten to horizontal plane so vertical head height doesn't bias the distance
            Vector3 headPos = playerA.position;
            Vector3 bPos = playerB.position;
            Vector3 delta = new Vector3(headPos.x - bPos.x, 0f, headPos.z - bPos.z);
            float dist = delta.magnitude;

            if (dist < minSafeDistanceMeters && dist > 0.0001f)
            {
                Vector3 pushDirection = delta.normalized;
                Vector3 safeHeadPos = bPos + pushDirection * minSafeDistanceMeters;
                safeHeadPos.y = headPos.y; // preserve head height

                // Move the rig root (not the head) so that the head ends up at safeHeadPos.
                // This works because the head is tracked relative to the rig root.
                Transform rigToMove = playerARigRoot != null ? playerARigRoot : playerA;
                Vector3 offset = safeHeadPos - headPos;
                rigToMove.position += offset;
            }
        }

        [Header("Simulated Player B Response")]
        [Tooltip("Delay (seconds) AFTER Player A re-enters the consent-request zone before Player B's queued response fires.")]
        public float playerBResponseDelay = 5f;

        [Tooltip("Optional TMP text floating above PlayerB showing the countdown.")]
        public TMPro.TextMeshPro playerBThinkingLabel;

        // Queued Player B decision set by the wall buttons. Fires only once Player A is back in range.
        private enum QueuedResponse { None, Accept, Reject }
        private QueuedResponse _queuedResponse = QueuedResponse.None;
        private Coroutine _queuedResponseRoutine;
        private float _queuedCountdownRemaining;
        private string _lastDecision = "—";

        /// <summary>Wall-button OnClick: queue an Accept from Player B. Fires after Player A re-enters range.</summary>
        public void SimulatePlayerBAccept()
        {
            _queuedResponse = QueuedResponse.Accept;
            Debug.Log("[Harness] Player B queued ACCEPT — will fire when Player A re-enters range.");
        }

        /// <summary>Wall-button OnClick: queue a Reject from Player B. Fires after Player A re-enters range.</summary>
        public void SimulatePlayerBReject()
        {
            _queuedResponse = QueuedResponse.Reject;
            Debug.Log("[Harness] Player B queued REJECT — will fire when Player A re-enters range.");
        }

        /// <summary>Wall-button OnClick: clear everything — cancel queued responses, reset cooldowns,
        /// rebuild the machine. Useful for resetting the demo between runs.</summary>
        public void SimulateReset()
        {
            Debug.Log("[Harness] Reset pressed — clearing state.");
            if (_queuedResponseRoutine != null) { StopCoroutine(_queuedResponseRoutine); _queuedResponseRoutine = null; }
            _queuedResponse = QueuedResponse.None;
            _sessionConsented = false;
            _cooldownEndTime = 0f;
            BuildMachine();
            if (consentUI != null) consentUI.Hide();
        }

        private void TryFireQueuedResponse()
        {
            if (_queuedResponse == QueuedResponse.None) return;
            if (_queuedResponseRoutine != null) return; // already counting down
            _queuedResponseRoutine = StartCoroutine(FireQueuedResponseAfterDelay());
        }

        private IEnumerator FireQueuedResponseAfterDelay()
        {
            Debug.Log($"[Harness] Queued Player B response ({_queuedResponse}) firing in {playerBResponseDelay}s");
            _queuedCountdownRemaining = playerBResponseDelay;
            while (_queuedCountdownRemaining > 0f)
            {
                if (playerBThinkingLabel != null)
                    playerBThinkingLabel.text = $"Player B is thinking…\n{Mathf.CeilToInt(_queuedCountdownRemaining)}";
                _queuedCountdownRemaining -= Time.deltaTime;
                yield return null;
            }
            if (playerBThinkingLabel != null) playerBThinkingLabel.text = string.Empty;

            if (Machine != null && Machine.State == ConsentState.Requested)
            {
                if (_queuedResponse == QueuedResponse.Accept)
                {
                    Machine.Accept(_idB);
                    _lastDecision = "ACCEPTED";
                }
                else if (_queuedResponse == QueuedResponse.Reject)
                {
                    Machine.Cancel(_idB);
                    _lastDecision = "REJECTED";
                }
            }

            _queuedResponse = QueuedResponse.None;
            _queuedResponseRoutine = null;
        }

        /// <summary>Returns a live status string for the wall status board.</summary>
        public string GetStatusReport()
        {
            float cooldownRemaining = Mathf.Max(0f, _cooldownEndTime - Time.time);
            string queuedInfo = _queuedResponse != QueuedResponse.None
                ? $"  (queued: {_queuedResponse})"
                : string.Empty;
            return
                $"STATE: {Machine?.State}{queuedInfo}\n" +
                $"LAST DECISION: {_lastDecision}\n" +
                $"COOLDOWN: {cooldownRemaining:F0}s\n" +
                $"DISTANCE: {CurrentDistance:F2} m";
        }

        /// <summary>
        /// Temporarily tints the consent panel's background to provide Accept/Reject visual feedback.
        /// Finds the first Image on the ConsentUI and flashes it to `color` for `durationSeconds`.
        /// </summary>
        private void FlashConsentPanel(Color color, float durationSeconds)
        {
            if (consentUI == null) return;
            var img = consentUI.GetComponentInChildren<Image>(true);
            if (img == null) return;
            StartCoroutine(FlashPanelCoroutine(img, color, durationSeconds));
        }

        private IEnumerator FlashPanelCoroutine(Image img, Color flashColor, float durationSeconds)
        {
            // Make sure the panel is active so the flash is visible even after state moved to Active/Terminated
            if (!img.gameObject.activeInHierarchy) img.gameObject.SetActive(true);
            Color original = img.color;
            img.color = flashColor;
            yield return new WaitForSeconds(durationSeconds);
            img.color = original;
        }

        private void WireConsentUI()
        {
            // Hook the ConsentUI's UnityEvents directly into the state machine
            consentUI.OnAccept.AddListener(() => Machine.Accept(_idA));
            consentUI.OnReject.AddListener(() => Machine.Cancel(_idB));
            consentUI.OnWithdraw.AddListener(() => Machine.Withdraw(_idA));
        }

        private void Update()
        {
            if (playerA == null || playerB == null)
            {
                if (Time.frameCount % 120 == 0)
                    Debug.LogWarning($"[Harness] Update early-return: playerA={(playerA == null ? "NULL" : "ok")} playerB={(playerB == null ? "NULL" : "ok")}");
                return;
            }

            float rawDist = Vector3.Distance(playerA.position, playerB.position);
            CurrentDistance = rawDist;

            // Range detection (bypassing ProximityService until we debug it).
            // Route through HandleRangeChanged so the "rebuild machine on leave" logic runs —
            // without it, a second approach after Terminated never retriggers the popup.
            bool shouldBeInRange = rawDist < maxRangeMeters;
            if (shouldBeInRange != _isInRange)
            {
                HandleRangeChanged(_idA, _idB, shouldBeInRange);
            }

            Machine.Tick();

            if (Input.GetKeyDown(KeyCode.R)) Machine.RequestConsent(_idB);
            if (Input.GetKeyDown(KeyCode.A)) Machine.Accept(_idA);
            if (Input.GetKeyDown(KeyCode.W)) Machine.Withdraw(_idB);
            if (Input.GetKeyDown(KeyCode.C)) Machine.Cancel(_idB);
            if (Input.GetKeyDown(KeyCode.X)) Machine.Withdraw(_idA);

            // Demo shortcuts for the wall buttons — press B to queue Accept, N for Reject.
            // Works whether or not VR controller ray-clicks are set up.
            if (Input.GetKeyDown(KeyCode.B)) SimulatePlayerBAccept();
            if (Input.GetKeyDown(KeyCode.N)) SimulatePlayerBReject();
        }

        // LateUpdate runs AFTER the XR rig's tracking update, so our push-back sticks.
        private void LateUpdate()
        {
            EnforceProtectedZone();
        }
    }

}
