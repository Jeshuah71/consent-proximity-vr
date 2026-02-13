using System;
using ConsentProximity.Core;

namespace ConsentProximity.StateMachine
{
    /// <summary>
    /// State Machine:
    /// Idle -> InRange -> Requested -> Active -> Terminated
    ///
    /// Key rules:
    /// - Handles timeouts, cancellations, distance exceed, re-enter range, duplicate requests,
    ///   and immediate termination if any participant withdraws consent.
    /// - It can just ask consent by being within InRange.
    /// - Requested expires for timeout.
    /// - Requested can be cancelled by requester.
    /// - If you exceed distance in Requested/Active -> Terminated (DistanceExceeded).
    /// - If anyone withdraws consent (Withdraw) -> Terminated immediately.
    /// - Duplicate request is rejected (no state change).
    /// </summary>
    public sealed class ConsentStateMachine
    {
        public event Action<ConsentState, ConsentState> OnStateChanged;
        public event Action<TerminationReason> OnTerminated;

        public ConsentState State { get; private set; } = ConsentState.Idle;
        public TerminationReason? LastTermination { get; private set; } = null;

        public ParticipantId A { get; }
        public ParticipantId B { get; }

        public ParticipantId? CurrentRequester => _hasRequest ? _requester : null;

        private readonly ConsentConfig _config;
        private readonly IClock _clock;
        private readonly IDistanceProvider _distance;

        private bool _inRange;

        private bool _hasRequest;
        private ParticipantId _requester;
        private float _requestStart;

        public ConsentStateMachine(
            ParticipantId a,
            ParticipantId b,
            ConsentConfig config,
            IClock clock,
            IDistanceProvider distanceProvider)
        {
            A = a;
            B = b;
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _distance = distanceProvider ?? throw new ArgumentNullException(nameof(distanceProvider));
        }

        /// <summary>
        /// The integration layer calls this when it detects if they are in range or not.
        /// </summary>
        public void SetInRange(bool inRange)
        {
            _inRange = inRange;

            // If we exit range during Requested/Active, we terminate immediately.
            if (!inRange && (State == ConsentState.Requested || State == ConsentState.Active))
            {
                Terminate(TerminationReason.DistanceExceeded);
                return;
            }

            if (State == ConsentState.Idle && inRange) ChangeState(ConsentState.InRange);
            else if (State == ConsentState.InRange && !inRange) ChangeState(ConsentState.Idle);

            // If we were already Terminated, the integration layer decides whether to reset with a new instance,
            // or maintain Terminated until explicit reset (for simplicity we keep Terminated).
        }

        /// <summary>
        /// Is called when someone requests consent.
        /// </summary>
        public bool RequestConsent(ParticipantId requester)
        {
            if (State != ConsentState.InRange || !_inRange) return false;
            if (!IsParticipant(requester)) return false;

            // Duplicate request: if there's already a request or active, reject.
            if (State == ConsentState.Requested || State == ConsentState.Active) return false;

            _hasRequest = true;
            _requester = requester;
            _requestStart = _clock.Now;

            ChangeState(ConsentState.Requested);
            return true;
        }

        /// <summary>
        /// The other participant accepts.
        /// </summary>
        public bool Accept(ParticipantId accepter)
        {
            if (State != ConsentState.Requested || !_hasRequest) return false;
            if (!IsParticipant(accepter)) return false;
            if (accepter == _requester) return false;

            if (IsDistanceExceeded())
            {
                Terminate(TerminationReason.DistanceExceeded);
                return false;
            }

            ChangeState(ConsentState.Active);
            return true;
        }

        /// <summary>
        /// The requester cancels the request.
        /// </summary>
        public bool Cancel(ParticipantId requester)
        {
            if (State != ConsentState.Requested || !_hasRequest) return false;
            if (requester != _requester) return false;

            Terminate(TerminationReason.Cancelled);
            return true;
        }

        /// <summary>
        /// Anyone withdraws consent. Must terminate immediately regardless of current state.
        /// </summary>
        public bool Withdraw(ParticipantId who)
        {
            if (!IsParticipant(who)) return false;

            if (State != ConsentState.Terminated)
            {
                Terminate(TerminationReason.WithdrawnConsent);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Call periodically (e.g. Update or FixedUpdate in integration).
        /// Makes enforcement of timeout and distance.
        /// </summary>
        public void Tick()
        {
            if (State == ConsentState.Requested && _hasRequest)
            {
                if ((_clock.Now - _requestStart) >= _config.requestTimeoutSeconds)
                {
                    Terminate(TerminationReason.Timeout);
                    return;
                }

                if (IsDistanceExceeded())
                {
                    Terminate(TerminationReason.DistanceExceeded);
                    return;
                }
            }
            else if (State == ConsentState.Active)
            {
                if (IsDistanceExceeded())
                {
                    Terminate(TerminationReason.DistanceExceeded);
                    return;
                }
            }
        }

        private bool IsParticipant(ParticipantId p) => p == A || p == B;

        private bool IsDistanceExceeded()
        {
            float d = _distance.GetDistanceMeters(A, B);
            return d > _config.maxRangeMeters;
        }

        private void ChangeState(ConsentState next)
        {
            if (State == next) return;
            var prev = State;
            State = next;
            OnStateChanged?.Invoke(prev, next);
        }

        private void Terminate(TerminationReason reason)
        {
            // Reset request flags
            _hasRequest = false;
            _requester = default;
            _requestStart = 0f;

            LastTermination = reason;

            var prev = State;
            State = ConsentState.Terminated;

            OnStateChanged?.Invoke(prev, State);
            OnTerminated?.Invoke(reason);
        }
    }
}
