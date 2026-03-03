using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace ConsentProximityFramework.Runtime.UI
{
    public class ConsentUI : MonoBehaviour
    {
        [Header("Events to connect later")]
        public UnityEvent OnAccept;
        public UnityEvent OnReject;
        public UnityEvent OnWithdraw;

        [Header("UI References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private GameObject withdrawRoot;

        [Header("Timeout")]
        [SerializeField] private float timeoutSeconds = 15f; // time out after 15 seconds

        private Coroutine timeoutRoutine;

        private void Awake()
        {
            Hide();
        }

        // Use to show UI in UNITY to test 
        private void Start()
        {
            Show();
        }

        public void Show() // makes panel visible!
        {
            if (panelRoot != null)
                panelRoot.SetActive(true);
            
            StartTimeout(); // adding the 15 sec timeout ft
        }

        public void Hide() // hides everything!
        {
            StopTimeout(); // to handle weird background timeout stuff 
            
            if (panelRoot != null)
                panelRoot.SetActive(false);

            if (withdrawRoot != null)
                withdrawRoot.SetActive(false);
        }

        public void Accept() // logs + triggers event
        {
            StopTimeout();
            Debug.Log("Consent Accepted");
            OnAccept?.Invoke();
        }

        public void Reject() // logs + triggers event
        {
            StopTimeout();
            Debug.Log("Consent Rejected");
            OnReject?.Invoke();
        }

        public void Withdraw() // logs + triggers event 
        {
            StopTimeout();
            Debug.Log("Consent Withdrawn");
            OnWithdraw?.Invoke();
        }
        private void StartTimeout() // starting the timeout
        {
            StopTimeout();
            timeoutRoutine = StartCoroutine(TimeoutCoroutine());
        }

        private void StopTimeout() // stopping the timeout
        {
            if (timeoutRoutine != null)
            {
                StopCoroutine(timeoutRoutine);
                timeoutRoutine = null;
            }
        }
        private IEnumerator TimeoutCoroutine()
        {
            float t = 0f; // unscaled so it still works of timescale changes (pause, slow-mo, etc)
            while (t < timeoutSeconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            Debug.Log($"Consent timed out after {timeoutSeconds}s --> auto reject");

            Reject(); // what actually triggers the rejection
        }
    }
}
