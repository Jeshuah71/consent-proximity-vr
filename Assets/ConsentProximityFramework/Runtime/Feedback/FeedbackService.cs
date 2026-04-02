using ConsentProximity.Core;
using ConsentProximityFramework.Runtime.Networking;
using UnityEngine;
using UnityEngine.Events;

namespace ConsentProximityFramework.Runtime.Feedback
{
    /// <summary>
    /// Minimal state-driven feedback surface for visuals and optional haptic hooks.
    /// </summary>
    public class FeedbackService : MonoBehaviour
    {
        [SerializeField] private ConsentFlowManager flowManager;

        [Header("State Indicators")]
        [SerializeField] private GameObject inRangeIndicator;
        [SerializeField] private GameObject requestedIndicator;
        [SerializeField] private GameObject activeIndicator;
        [SerializeField] private GameObject terminatedIndicator;

        [Header("Optional Hooks")]
        [SerializeField] private UnityEvent onRequestPulse;
        [SerializeField] private UnityEvent onAcceptPulse;
        [SerializeField] private UnityEvent onTerminatePulse;

        private void OnEnable()
        {
            if (flowManager != null)
            {
                flowManager.OnStateChanged += ApplyState;
                ApplyState(flowManager.CurrentState);
            }
        }

        private void OnDisable()
        {
            if (flowManager != null)
            {
                flowManager.OnStateChanged -= ApplyState;
            }
        }

        public void ApplyState(ConsentState state)
        {
            SetActive(inRangeIndicator, state == ConsentState.InRange);
            SetActive(requestedIndicator, state == ConsentState.Requested);
            SetActive(activeIndicator, state == ConsentState.Active);
            SetActive(terminatedIndicator, state == ConsentState.Terminated);

            switch (state)
            {
                case ConsentState.Requested:
                    onRequestPulse?.Invoke();
                    break;
                case ConsentState.Active:
                    onAcceptPulse?.Invoke();
                    break;
                case ConsentState.Terminated:
                    onTerminatePulse?.Invoke();
                    break;
            }
        }

        private static void SetActive(GameObject target, bool visible)
        {
            if (target != null)
            {
                target.SetActive(visible);
            }
        }
    }
}
