using ConsentProximity.Core;
using UnityEngine;

namespace ConsentProximity.TestHarness
{
    /// <summary>
    /// Moves a dummy player toward a target, respecting consent state:
    ///   Idle        → manual movement (Space to toggle)
    ///   InRange     → auto-stop (waiting for consent flow)
    ///   Requested   → auto-stop (pending response)
    ///   Active      → resume movement (consent granted)
    ///   Terminated  → auto-reset to spawn point
    /// </summary>
    public class DummyPlayerMover : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;

        [Header("Settings")]
        public float moveSpeed = 0.5f;
        public float stopDistance = 0.5f;
        public float resetDelay = 1.5f;

        [Header("Harness (auto-found if empty)")]
        public HarnessController harness;

        private Vector3 _startPosition;
        private float _resetTimer;

        public bool IsMoving { get; set; }

        void Start()
        {
            _startPosition = transform.position;

            if (harness == null)
            {
                harness = FindFirstObjectByType<HarnessController>();
            }
        }

        void Update()
        {
            ConsentState state = harness != null ? harness.Machine.State : ConsentState.Idle;

            // Terminated: wait briefly, then reset to spawn
            if (state == ConsentState.Terminated)
            {
                IsMoving = false;
                _resetTimer += Time.deltaTime;
                if (_resetTimer >= resetDelay)
                {
                    ResetToSpawn();
                    _resetTimer = 0f;
                }
                return;
            }

            _resetTimer = 0f;

            // Space toggles manual movement
            if (Input.GetKeyDown(KeyCode.Space))
            {
                IsMoving = !IsMoving;
            }

            // InRange / Requested: freeze, wait for consent
            if (state == ConsentState.InRange || state == ConsentState.Requested)
            {
                return;
            }

            // Idle or Active: allow movement
            if (!IsMoving || target == null) return;

            float dist = Vector3.Distance(transform.position, target.position);
            if (dist <= stopDistance) return;

            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
        }

        public void ResetToSpawn()
        {
            transform.position = _startPosition;
            IsMoving = false;
        }
    }
}