using System;
using ConsentProximity.Core;
using ConsentProximity.StateMachine;
using UnityEngine;

namespace ConsentProximityFramework.Runtime.Networking
{
    /// <summary>
    /// Thin Unity-facing wrapper around the core consent state machine.
    /// This keeps networking and samples on the same logic path.
    /// </summary>
    public class ConsentFlowManager : MonoBehaviour
    {
        [SerializeField] private float maxRangeMeters = 2f;
        [SerializeField] private float requestTimeoutSeconds = 8f;

        public event Action<ConsentState> OnStateChanged;

        public ConsentState CurrentState => _machine.State;

        private readonly ParticipantId _requester = new ParticipantId("Requester");
        private readonly ParticipantId _responder = new ParticipantId("Responder");

        private ConsentStateMachine _machine;
        private ManualDistanceProvider _distanceProvider;
        private readonly RuntimeClock _clock = new RuntimeClock();
        private bool _isInRange;

        private void Awake()
        {
            EnsureMachine();
        }

        public void EnterProximity()
        {
            EnsureMachine();
            _isInRange = true;
            _distanceProvider.DistanceMeters = 0f;
            _machine.SetInRange(true);
        }

        public void ExitProximity()
        {
            EnsureMachine();
            _isInRange = false;
            _distanceProvider.DistanceMeters = maxRangeMeters + 1f;
            _machine.SetInRange(false);

            if (_machine.State == ConsentState.Terminated)
            {
                ResetMachine(false);
            }
        }

        public void RequestConsent()
        {
            EnsureMachine();
            _machine.RequestConsent(_requester);
        }

        public void OnConsentRequested()
        {
            RequestConsent();
        }

        public void AcceptConsent()
        {
            EnsureMachine();
            _machine.Accept(_responder);
        }

        public void OnConsentAccepted()
        {
            AcceptConsent();
        }

        public void RejectConsent()
        {
            if (CurrentState != ConsentState.Requested)
            {
                Debug.Log("Reject blocked: no pending request.");
                return;
            }

            ResetMachine(_isInRange);
        }

        public void OnConsentRejected()
        {
            RejectConsent();
        }

        public void WithdrawConsent()
        {
            EnsureMachine();
            if (_machine.Withdraw(_responder))
            {
                ResetMachine(false);
            }
        }

        public void OnConsentWithdrawn()
        {
            WithdrawConsent();
        }

        public void TerminateInteraction()
        {
            EnsureMachine();
            if (_machine.Withdraw(_requester))
            {
                ResetMachine(false);
            }
        }

        public void OnConsentTerminated(string reason = null)
        {
            if (!string.IsNullOrWhiteSpace(reason))
            {
                Debug.Log($"Remote terminate reason: {reason}");
            }

            TerminateInteraction();
        }

        public void OnRemoteDisconnect()
        {
            EnsureMachine();

            if (CurrentState == ConsentState.Requested || CurrentState == ConsentState.Active)
            {
                _machine.Withdraw(_requester);
            }

            ResetMachine(false);
        }

        public void OnRemoteDisconnect(ulong _)
        {
            OnRemoteDisconnect();
        }

        private void EnsureMachine()
        {
            if (_machine != null)
            {
                return;
            }

            _distanceProvider = new ManualDistanceProvider();
            BuildMachine(_isInRange);
        }

        private void BuildMachine(bool inRange)
        {
            var config = new ConsentConfig
            {
                maxRangeMeters = maxRangeMeters,
                requestTimeoutSeconds = requestTimeoutSeconds
            };

            _machine = new ConsentStateMachine(_requester, _responder, config, _clock, _distanceProvider);
            _machine.OnStateChanged += HandleMachineStateChanged;
            _distanceProvider.DistanceMeters = inRange ? 0f : maxRangeMeters + 1f;
            _machine.SetInRange(inRange);
        }

        private void ResetMachine(bool inRange)
        {
            _isInRange = inRange;

            if (_machine != null)
            {
                _machine.OnStateChanged -= HandleMachineStateChanged;
            }

            BuildMachine(inRange);
            if (!inRange)
            {
                EmitStateChanged(CurrentState);
            }
        }

        private void HandleMachineStateChanged(ConsentState _, ConsentState next)
        {
            EmitStateChanged(next);
        }

        private void EmitStateChanged(ConsentState state)
        {
            Debug.Log($"Consent state changed to: {state}");
            OnStateChanged?.Invoke(state);
        }

        private sealed class ManualDistanceProvider : IDistanceProvider
        {
            public float DistanceMeters { get; set; } = float.MaxValue;

            public float GetDistanceMeters(ParticipantId a, ParticipantId b)
            {
                return DistanceMeters;
            }
        }

        private sealed class RuntimeClock : IClock
        {
            public float Now => Time.unscaledTime;
        }
    }
}
