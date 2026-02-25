using UnityEngine;
using System;

namespace ConsentProximityFramework.Runtime.Networking
{
    /// <summary>
    /// Manages the consent flow state for an interaction.
    /// Central authority for consent logic.
    /// </summary>
    public class ConsentFlowManager : MonoBehaviour
    {
        [SerializeField]
        private ConsentState currentState = ConsentState.Idle;

        /// <summary>
        /// Event triggered whenever the consent state changes.
        /// </summary>
        public event Action<ConsentState> OnStateChanged;

        public ConsentState CurrentState => currentState;

        /// <summary>
        /// Sets a new consent state.
        /// All transitions must go through here.
        /// </summary>
        private void SetState(ConsentState newState)
        {
            if (currentState == newState)
                return;

            currentState = newState;
            Debug.Log($"Consent state changed to: {currentState}");

            OnStateChanged?.Invoke(currentState);
        }

        // -----------------------------
        // PROXIMITY
        // -----------------------------

        public void EnterProximity()
        {
            if (currentState == ConsentState.Idle)
                SetState(ConsentState.InRange);
        }

        public void ExitProximity()
        {
            if (currentState != ConsentState.Idle)
                SetState(ConsentState.Idle);
        }

        // -----------------------------
        // REQUEST FLOW
        // -----------------------------

        public void RequestConsent()
        {
            if (currentState != ConsentState.InRange)
            {
                Debug.Log("Request blocked: not in range.");
                return;
            }

            SetState(ConsentState.Requested);
        }

        public void AcceptConsent()
        {
            if (currentState != ConsentState.Requested)
            {
                Debug.Log("Accept blocked: no pending request.");
                return;
            }

            SetState(ConsentState.Active);
        }

        public void RejectConsent()
        {
            if (currentState != ConsentState.Requested)
            {
                Debug.Log("Reject blocked: no pending request.");
                return;
            }

            SetState(ConsentState.InRange);
        }

        // -----------------------------
        // WITHDRAW / TERMINATE
        // -----------------------------

        public void WithdrawConsent()
        {
            if (currentState != ConsentState.Requested &&
                currentState != ConsentState.Active)
            {
                Debug.Log("Withdraw blocked: no active or pending interaction.");
                return;
            }

            SetState(ConsentState.Terminated);
        }

        public void TerminateInteraction()
        {
            if (currentState != ConsentState.Requested &&
                currentState != ConsentState.Active)
            {
                Debug.Log("Terminate blocked: no active interaction.");
                return;
            }

            SetState(ConsentState.Terminated);
        }

        // -----------------------------
        // DISCONNECT SAFETY
        // -----------------------------

        public void OnRemoteDisconnect()
        {
            if (currentState == ConsentState.Active ||
                currentState == ConsentState.Requested)
            {
                SetState(ConsentState.Terminated);
            }

            SetState(ConsentState.Idle);
        }
    }
}