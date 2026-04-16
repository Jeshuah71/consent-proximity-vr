using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace ConsentProximityFramework.Runtime.UI
{
    /// <summary>
    /// Backward-compatible UI component kept for existing scene references in the runtime assembly.
    /// </summary>
    public class ConsentUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject requestPanel;
        [SerializeField] private GameObject withdrawPanel;

        [Header("Behavior")]
        [SerializeField] private float timeoutSeconds = 15f;
        [SerializeField] private bool showOnStart;

        [Header("Events")]
        public UnityEvent OnAccept;
        public UnityEvent OnReject;
        public UnityEvent OnWithdraw;
        public UnityEvent OnTimedOut;

        private Coroutine _timeoutRoutine;

        private void Awake()
        {
            HideAll();
        }

        private void Start()
        {
            if (showOnStart)
            {
                Show();
            }
        }

        public void Show()
        {
            SetRequestVisible(true);
            SetWithdrawVisible(false);
            RestartTimeout();
        }

        public void ShowWithdrawOnly()
        {
            StopTimeout();
            SetRequestVisible(false);
            SetWithdrawVisible(true);
        }

        public void Hide()
        {
            HideAll();
        }

        public void Accept()
        {
            StopTimeout();
            OnAccept?.Invoke();
        }

        public void Reject()
        {
            StopTimeout();
            OnReject?.Invoke();
            HideAll();
        }

        public void Withdraw()
        {
            StopTimeout();
            OnWithdraw?.Invoke();
            HideAll();
        }

        private void HideAll()
        {
            StopTimeout();
            SetRequestVisible(false);
            SetWithdrawVisible(false);
        }

        private void RestartTimeout()
        {
            StopTimeout();
            _timeoutRoutine = StartCoroutine(TimeoutCoroutine());
        }

        private void StopTimeout()
        {
            if (_timeoutRoutine == null)
            {
                return;
            }

            StopCoroutine(_timeoutRoutine);
            _timeoutRoutine = null;
        }

        private IEnumerator TimeoutCoroutine()
        {
            float elapsed = 0f;
            while (elapsed < timeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            OnTimedOut?.Invoke();
            Reject();
        }

        private void SetRequestVisible(bool visible)
        {
            if (requestPanel != null)
            {
                requestPanel.SetActive(visible);
            }
        }

        private void SetWithdrawVisible(bool visible)
        {
            if (withdrawPanel != null)
            {
                withdrawPanel.SetActive(visible);
            }
        }
    }
}
