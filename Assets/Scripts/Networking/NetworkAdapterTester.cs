using Unity.Netcode;
using UnityEngine;

public class NetworkAdapterTester : MonoBehaviour
{
    [SerializeField] private NgoNetworkAdapter adapter;
    [SerializeField] private string sessionId = "session-001";
    [SerializeField] private string rejectReason = "ManualReject";
    [SerializeField] private string withdrawReason = "ManualWithdraw";
    [SerializeField] private string terminateReason = "ManualTerminate";

    private void Update()
    {
        if (adapter == null || NetworkManager.Singleton == null)
        {
            return;
        }

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        ulong targetClientId = ResolveTargetClientId();

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            adapter.SendConsentRequest(sessionId, targetClientId);
            Debug.Log($"[AdapterTester] Sent Request -> target={targetClientId}");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            adapter.SendAccept(sessionId, targetClientId);
            Debug.Log($"[AdapterTester] Sent Accept -> target={targetClientId}");
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            adapter.SendReject(sessionId, targetClientId, rejectReason);
            Debug.Log($"[AdapterTester] Sent Reject -> target={targetClientId}");
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            adapter.SendWithdraw(sessionId, targetClientId, withdrawReason);
            Debug.Log($"[AdapterTester] Sent Withdraw -> target={targetClientId}");
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            adapter.SendTerminate(sessionId, targetClientId, terminateReason);
            Debug.Log($"[AdapterTester] Sent Terminate -> target={targetClientId}");
        }
    }

    private static ulong ResolveTargetClientId()
    {
        var nm = NetworkManager.Singleton;

        if (nm.IsHost)
        {
            foreach (ulong clientId in nm.ConnectedClientsIds)
            {
                if (clientId != nm.LocalClientId)
                {
                    return clientId;
                }
            }

            // Fallback when no remote client connected yet.
            return nm.LocalClientId;
        }

        // Client targets server by default.
        return NetworkManager.ServerClientId;
    }

    private void OnGUI()
    {
        if (NetworkManager.Singleton == null) return;

        GUI.Label(new Rect(20, 160, 520, 25), "Adapter Tester: [1]Request [2]Accept [3]Reject [4]Withdraw [5]Terminate");
        GUI.Label(new Rect(20, 185, 520, 25), $"SessionId: {sessionId}");
    }
}
