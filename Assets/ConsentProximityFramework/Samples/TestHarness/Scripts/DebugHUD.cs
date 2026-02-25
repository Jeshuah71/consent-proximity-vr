using UnityEngine;
using TMPro;

namespace ConsentProximity.TestHarness
{
    public class DebugHUD : MonoBehaviour
    {
        [Header("References")]
        public HarnessController harness;
        public DummyPlayerMover mover;
        public TextMeshProUGUI hudText;

        void Update()
        {
            if (harness == null || hudText == null) return;

            var machine = harness.Machine;
            if (machine == null) return;

            string termination = machine.LastTermination.HasValue
                ? machine.LastTermination.Value.ToString()
                : "None";

            hudText.text =
                $"─── CONSENT PROXIMITY DEBUG ───\n" +
                $"Distance   : {harness.CurrentDistance:F2} m\n" +
                $"Max Range  : {harness.maxRangeMeters:F2} m\n" +
                $"State      : {machine.State}\n" +
                $"Termination: {termination}\n" +
                $"Requester  : {machine.CurrentRequester?.ToString() ?? "—"}\n" +
                $"Mover : {(mover != null ? (mover.IsMoving ? "MOVING" : "STOPPED") : "N/A")}\n" +
                $"\n─── Controls ───\n" +
                $"[SPACE] Toggle Player A auto-move\n" +
                $"[R]  Request consent (A→B)\n" +
                $"[A]  Accept (B)\n" +
                $"[W]  Withdraw (A)\n" +
                $"[C]  Cancel (A)";
        }
    }
}