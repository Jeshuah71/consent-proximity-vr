using System.Collections.Generic;
using ConsentProximityFramework.Runtime.Networking;
using Unity.Netcode;
using UnityEngine;

public class NgoNetworkAdapter : NetworkBehaviour, INetworkAdapter
{
    [SerializeField] private ConsentFlowManager flowManager;

    private ulong _localMessageCounter;
    private readonly SessionAuthorityRegistry _authorityRegistry = new SessionAuthorityRegistry();

    // sessionId -> senderClientId -> lastMessageId
    private readonly Dictionary<string, Dictionary<ulong, ulong>> _lastProcessed =
        new Dictionary<string, Dictionary<ulong, ulong>>();

    public override void OnNetworkSpawn()
    {
        if (NetworkManager != null)
        {
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager != null)
        {
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    public void SendConsentRequest(string sessionId, ulong targetClientId)
    {
        SendToServer(ConsentNetMessageType.Request, sessionId, targetClientId, null);
    }

    public void SendAccept(string sessionId, ulong targetClientId)
    {
        SendToServer(ConsentNetMessageType.Accept, sessionId, targetClientId, null);
    }

    public void SendReject(string sessionId, ulong targetClientId, string reason = null)
    {
        SendToServer(ConsentNetMessageType.Reject, sessionId, targetClientId, reason);
    }

    public void SendWithdraw(string sessionId, ulong targetClientId, string reason = null)
    {
        SendToServer(ConsentNetMessageType.Withdraw, sessionId, targetClientId, reason);
    }

    public void SendTerminate(string sessionId, ulong targetClientId, string reason = null)
    {
        SendToServer(ConsentNetMessageType.Terminate, sessionId, targetClientId, reason);
    }

    private void SendToServer(ConsentNetMessageType type, string sessionId, ulong targetClientId, string reason)
    {
        if (NetworkManager == null)
        {
            Debug.LogWarning("[NgoNetworkAdapter] NetworkManager is null.");
            return;
        }

        var msg = new ConsentNetMessage
        {
            Type = type,
            SessionId = sessionId,
            MessageId = ++_localMessageCounter,
            SenderClientId = NetworkManager.LocalClientId,
            Reason = reason ?? string.Empty,
            SentAt = NetworkManager.ServerTime.Time
        };

        SubmitMessageServerRpc(msg, targetClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitMessageServerRpc(
        ConsentNetMessage msg,
        ulong targetClientId,
        ServerRpcParams rpcParams = default)
    {
        if (NetworkManager == null || !IsServer)
        {
            Debug.LogWarning("[NgoNetworkAdapter] Server RPC received while not in server context.");
            return;
        }

        ulong rpcSender = rpcParams.Receive.SenderClientId;
        if (rpcSender != msg.SenderClientId)
        {
            Debug.LogWarning($"[NgoNetworkAdapter] Sender mismatch dropped. RpcSender={rpcSender} MsgSender={msg.SenderClientId}");
            return;
        }

        if (!NetworkManager.ConnectedClients.ContainsKey(targetClientId))
        {
            Debug.LogWarning($"[NgoNetworkAdapter] Invalid target dropped. Target={targetClientId}");
            return;
        }

        string sessionId = msg.SessionId.ToString();
        if (!_authorityRegistry.IsAuthorized(msg.Type, sessionId, msg.SenderClientId, targetClientId))
        {
            Debug.LogWarning($"[NgoNetworkAdapter] Unauthorized message dropped. Type={msg.Type} Session={sessionId} Sender={msg.SenderClientId} Target={targetClientId}");
            return;
        }

        var sendParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { targetClientId }
            }
        };

        Debug.Log($"[NgoNetworkAdapter] Forwarding message. Type={msg.Type} Session={sessionId} MsgId={msg.MessageId} Sender={msg.SenderClientId} Target={targetClientId}");
        ReceiveMessageClientRpc(msg, sendParams);
    }

    [ClientRpc]
    private void ReceiveMessageClientRpc(ConsentNetMessage msg, ClientRpcParams clientRpcParams = default)
    {
        if (IsDuplicateOrOld(msg))
        {
            return;
        }

        if (flowManager == null)
        {
            Debug.LogWarning("[NgoNetworkAdapter] ConsentFlowManager is not assigned.");
            return;
        }

        // Route only through FlowManager API. Do not mutate state directly here.
        switch (msg.Type)
        {
            case ConsentNetMessageType.Request:
                flowManager.OnConsentRequested();
                break;
            case ConsentNetMessageType.Accept:
                flowManager.OnConsentAccepted();
                break;
            case ConsentNetMessageType.Reject:
                flowManager.OnConsentRejected();
                break;
            case ConsentNetMessageType.Withdraw:
                flowManager.OnConsentWithdrawn();
                break;
            case ConsentNetMessageType.Terminate:
                flowManager.OnConsentTerminated(msg.Reason.ToString());
                break;
        }
    }

    private bool IsDuplicateOrOld(ConsentNetMessage msg)
    {
        string session = msg.SessionId.ToString();

        if (!_lastProcessed.TryGetValue(session, out var bySender))
        {
            bySender = new Dictionary<ulong, ulong>();
            _lastProcessed[session] = bySender;
        }

        if (bySender.TryGetValue(msg.SenderClientId, out ulong lastSeen) && msg.MessageId <= lastSeen)
        {
            Debug.Log($"[NgoNetworkAdapter] Ignored duplicate/old message. Session={session} Sender={msg.SenderClientId} MsgId={msg.MessageId} Last={lastSeen}");
            return true;
        }

        bySender[msg.SenderClientId] = msg.MessageId;
        return false;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        _authorityRegistry.RemoveSessionsForClient(clientId);

        if (flowManager == null)
        {
            return;
        }

        // Trigger remote disconnect handling through flow manager path.
        flowManager.OnRemoteDisconnect(clientId);
    }
}
