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

        private void Awake()
        {
            Hide();
        }

        public void Show() // makes panel visible!
        {
            if (panelRoot != null)
                panelRoot.SetActive(true);
        }

        public void Hide() // hides everything!
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            if (withdrawRoot != null)
                withdrawRoot.SetActive(false);
        }

        public void Accept() // logs + triggers event
        {
            Debug.Log("Consent Accepted");
            OnAccept?.Invoke();
        }

        public void Reject() // logs + triggers event
        {
            Debug.Log("Consent Rejected");
            OnReject?.Invoke();
        }

        public void Withdraw() // logs + triggers event 
        {
            Debug.Log("Consent Withdrawn");
            OnWithdraw?.Invoke();
        }
    }
}
