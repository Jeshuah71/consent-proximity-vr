using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace ConsentProximityFramework.Runtime.ConsentUI
{
    /// <summary>
    /// Local consent prompt with explicit accept/reject actions and an always-available withdraw control while active.
    /// </summary>
    public class ConsentUIPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject requestPanel;
        [SerializeField] private GameObject withdrawPanel;

        [Header("Behavior")]
        [SerializeField] private float timeoutSeconds = 15f;
        [SerializeField] private bool showOnStart;

        [Header("Events")]
        [SerializeField] private UnityEvent onAccept;
        [SerializeField] private UnityEvent onReject;
        [SerializeField] private UnityEvent onWithdraw;
        [SerializeField] private UnityEvent onTimedOut;

        private Coroutine _timeoutRoutine;

        private void Awake()
        {
            HideAll();
        }

        private void Start()
        {
            if (showOnStart)
            {
                ShowRequest();
            }
        }

        public void ShowRequest()
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

        public void HideAll()
        {
            StopTimeout();
            SetRequestVisible(false);
            SetWithdrawVisible(false);
        }

        public void Accept()
        {
            StopTimeout();
            onAccept?.Invoke();
        }

        public void Reject()
        {
            StopTimeout();
            onReject?.Invoke();
            HideAll();
        }

        public void Withdraw()
        {
            StopTimeout();
            onWithdraw?.Invoke();
            HideAll();
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

            onTimedOut?.Invoke();
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
