using UnityEngine;

namespace ConsentProximity.TestHarness
{
    public class DummyPlayerMover : MonoBehaviour
    {
        [Header("Target")]
        public Transform target;

        [Header("Settings")]
        public float moveSpeed = 0.5f;
        public float stopDistance = 0.5f;

        private Vector3 _startPosition;
        public bool IsMoving { get; set; } = false;

        void Start()
        {
            _startPosition = transform.position;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                IsMoving = !IsMoving;

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