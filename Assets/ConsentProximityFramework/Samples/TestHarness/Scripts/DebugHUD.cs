using UnityEngine;
using TMPro;

namespace ConsentProximity.TestHarness
{
    public class DebugHUD : MonoBehaviour
    {
        [Header("References")]
        public HarnessController harness;
        public DummyPlayerMover mover;

        [Tooltip("TextMeshProUGUI (for Canvas-based HUD).")]
        public TextMeshProUGUI hudText;

        [Tooltip("World-space TextMeshPro (for in-VR status board mounted on a wall). Use either this OR hudText.")]
        public TextMeshPro worldText;

        void Update()
        {
            if (harness == null) return;
            var machine = harness.Machine;
            if (machine == null) return;

            // Short, VR-friendly status report (uses the new HarnessController.GetStatusReport()).
            string statusReport = harness.GetStatusReport();

            if (worldText != null) worldText.text = statusReport;
            if (hudText != null) hudText.text = statusReport;
        }
    }
}