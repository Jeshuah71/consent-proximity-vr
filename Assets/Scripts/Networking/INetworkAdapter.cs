public interface INetworkAdapter
{
    void SendConsentRequest(string sessionId, ulong targetClientId);
    void SendAccept(string sessionId, ulong targetClientId);
    void SendReject(string sessionId, ulong targetClientId, string reason = null);
    void SendWithdraw(string sessionId, ulong targetClientId, string reason = null);
    void SendTerminate(string sessionId, ulong targetClientId, string reason = null);
}
