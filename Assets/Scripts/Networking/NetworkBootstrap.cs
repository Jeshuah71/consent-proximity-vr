using Unity.Netcode;
using UnityEngine;

public class NetworkBootstrap : MonoBehaviour
{
    private NetworkManager _networkManager;

    private void Awake()
    {
        _networkManager = NetworkManager.Singleton;
        if (_networkManager == null)
        {
            Debug.LogError("[NetworkBootstrap] NetworkManager.Singleton not found.");
            return;
        }

        _networkManager.OnClientConnectedCallback += OnClientConnected;
        _networkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnDestroy()
    {
        if (_networkManager == null) return;
        {
            _networkManager.OnClientConnectedCallback -= OnClientConnected;
            _networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnGUI()
    {
        if (_networkManager == null) return;

        if (!_networkManager.IsClient && !_networkManager.IsServer)
        {
            if (GUI.Button(new Rect(20, 20, 140, 35), "Start Host"))
            {
                _networkManager.StartHost();
            }

            if (GUI.Button(new Rect(20, 65, 140, 35), "Start Client"))
            {
                _networkManager.StartClient();
            }

            if (GUI.Button(new Rect(20, 110, 140, 35), "Start Server"))
            {
                _networkManager.StartServer();
            }

            return;
        }

        string mode = _networkManager.IsHost ? "Host" : (_networkManager.IsServer ? "Server" : "Client");
        GUI.Label(new Rect(20, 20, 420, 30), $"Mode: {mode} | LocalClientId: {_networkManager.LocalClientId}");

        if (GUI.Button(new Rect(20, 65, 140, 35), "Shutdown"))
        {
            _networkManager.Shutdown();
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[NGO] Client connected: {clientId}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"[NGO] Client disconnected: {clientId}");
    }
}
