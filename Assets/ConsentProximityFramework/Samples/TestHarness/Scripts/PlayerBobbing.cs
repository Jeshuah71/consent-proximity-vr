using UnityEngine;

namespace ConsentProximity.TestHarness
{
    /// <summary>
    /// Adds a gentle idle bobbing animation to Player B's capsule.
    /// Attach directly to the PlayerB GameObject.
    /// </summary>
    public class PlayerBobbing : MonoBehaviour
    {
        [Tooltip("How far up/down the bob travels in metres. Keep this small (0.03–0.08).")]
        public float amplitude = 0.05f;

        [Tooltip("How many full bobs per second.")]
        public float frequency = 0.8f;

        private Vector3 _startPos;

        private void Start()
        {
            _startPos = transform.position;
        }

        private void Update()
        {
            float newY = _startPos.y + Mathf.Sin(Time.time * frequency * 2f * Mathf.PI) * amplitude;
            transform.position = new Vector3(_startPos.x, newY, _startPos.z);
        }
    }
}
