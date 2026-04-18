using UnityEngine;
using ConsentProximity.Core;
using ConsentProximity.StateMachine;

namespace ConsentProximity.TestHarness
{
    /// <summary>
    /// Animates the floor consent ring based on the current consent state.
    /// Attach to the ZoneRing GameObject.
    /// </summary>
    public class ZoneRingPulse : MonoBehaviour
    {
        [Header("References")]
        public HarnessController harness;

        [Header("Scale")]
        [Tooltip("Base XZ scale of the ring. Overrides whatever is set in the Transform.")]
        public float baseScale = 5f;

        [Tooltip("How much the ring grows/shrinks while pulsing (Requested state).")]
        public float pulseAmount = 0.4f;

        [Tooltip("Pulse speed in cycles per second.")]
        public float pulseSpeed = 0.8f;

        [Header("Colors")]
        public Color idleColor       = new Color(0.55f, 0.55f, 0.55f, 0.50f);
        public Color inRangeColor    = new Color(0.9f,  0.8f,  0.1f,  0.45f); // yellow
        public Color requestedColor  = new Color(0.9f,  0.6f,  0.0f,  0.70f); // amber
        public Color activeColor     = new Color(0.2f,  0.85f, 0.3f,  0.65f); // green
        public Color terminatedColor = new Color(0.8f,  0.2f,  0.2f,  0.30f); // red fade

        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;

        private void Start()
        {
            _renderer  = GetComponent<Renderer>();
            _propBlock = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (harness == null || harness.Machine == null) return;

            var state = harness.Machine.State;
            float scale = baseScale;
            Color color = idleColor;

            switch (state)
            {
                case ConsentState.InRange:
                    scale = baseScale;
                    color = inRangeColor;
                    break;

                case ConsentState.Requested:
                    // Smooth pulse — grows and shrinks continuously
                    float pulse = Mathf.Sin(Time.time * pulseSpeed * Mathf.PI) * pulseAmount;
                    scale = baseScale + pulse;
                    color = requestedColor;
                    break;

                case ConsentState.Active:
                    scale = baseScale;
                    color = activeColor;
                    break;

                case ConsentState.Terminated:
                    // Only flash red briefly — after that treat it like idle visually
                    // (machine rebuilds on re-entry anyway, so red permanently looks wrong)
                    float idlePulse2 = Mathf.Sin(Time.time * 0.5f * Mathf.PI) * 0.15f;
                    scale = baseScale + idlePulse2;
                    color = idleColor;
                    break;

                default: // Idle — gentle slow pulse so the ring is always alive
                    float idlePulse = Mathf.Sin(Time.time * 0.5f * Mathf.PI) * 0.15f;
                    scale = baseScale + idlePulse;
                    color = idleColor;
                    break;
            }

            // Apply scale on XZ only — keep Y flat
            transform.localScale = new Vector3(scale, transform.localScale.y, scale);

            // Apply color via property block (non-destructive — doesn't create new materials)
            if (_renderer != null)
            {
                _renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_Color", color);
                _renderer.SetPropertyBlock(_propBlock);
            }
        }
    }
}
