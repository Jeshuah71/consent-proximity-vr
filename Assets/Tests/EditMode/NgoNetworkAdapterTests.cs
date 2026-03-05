using System.Reflection;
using ConsentProximityFramework.Runtime.Networking;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;

public class NgoNetworkAdapterTests
{
    private GameObject _adapterGo;
    private GameObject _flowGo;
    private NgoNetworkAdapter _adapter;
    private ConsentFlowManager _flow;

    [SetUp]
    public void SetUp()
    {
        _adapterGo = new GameObject("NgoNetworkAdapterTests_Adapter");
        _adapter = _adapterGo.AddComponent<NgoNetworkAdapter>();

        _flowGo = new GameObject("NgoNetworkAdapterTests_Flow");
        _flow = _flowGo.AddComponent<ConsentFlowManager>();

        typeof(NgoNetworkAdapter)
            .GetField("flowManager", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.SetValue(_adapter, _flow);
    }

    [TearDown]
    public void TearDown()
    {
        if (_adapterGo != null) Object.DestroyImmediate(_adapterGo);
        if (_flowGo != null) Object.DestroyImmediate(_flowGo);
    }

    [Test]
    public void ReceiveRequest_TransitionsFlowToRequested()
    {
        _flow.EnterProximity();
        Assert.AreEqual(ConsentState.InRange, _flow.CurrentState);

        var msg = CreateMessage(ConsentNetMessageType.Request, "s1", 1, 11);
        InvokeReceive(msg);

        Assert.AreEqual(ConsentState.Requested, _flow.CurrentState);
    }

    [Test]
    public void ReceiveDuplicateMessage_IsIgnored()
    {
        _flow.EnterProximity();

        var msg = CreateMessage(ConsentNetMessageType.Request, "s1", 1, 11);
        InvokeReceive(msg);
        Assert.AreEqual(ConsentState.Requested, _flow.CurrentState);

        // Duplicate message id from same sender/session should be ignored.
        InvokeReceive(msg);
        Assert.AreEqual(ConsentState.Requested, _flow.CurrentState);
    }

    [Test]
    public void ReceiveOlderMessage_FromSameSenderSession_IsIgnored()
    {
        _flow.EnterProximity();

        InvokeReceive(CreateMessage(ConsentNetMessageType.Request, "s1", 2, 11));
        Assert.AreEqual(ConsentState.Requested, _flow.CurrentState);

        // Older message id should be treated as stale and ignored.
        InvokeReceive(CreateMessage(ConsentNetMessageType.Request, "s1", 1, 11));
        Assert.AreEqual(ConsentState.Requested, _flow.CurrentState);
    }

    [Test]
    public void ReceiveAccept_TransitionsRequestedToActive()
    {
        _flow.EnterProximity();
        InvokeReceive(CreateMessage(ConsentNetMessageType.Request, "s1", 1, 11));
        Assert.AreEqual(ConsentState.Requested, _flow.CurrentState);

        InvokeReceive(CreateMessage(ConsentNetMessageType.Accept, "s1", 2, 11));
        Assert.AreEqual(ConsentState.Active, _flow.CurrentState);
    }

    [Test]
    public void Disconnect_RoutesThroughFlowManager()
    {
        _flow.EnterProximity();
        _flow.RequestConsent();
        Assert.AreEqual(ConsentState.Requested, _flow.CurrentState);

        typeof(NgoNetworkAdapter)
            .GetMethod("OnClientDisconnected", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.Invoke(_adapter, new object[] { 99UL });

        // Flow manager ends in Idle after handling remote disconnect.
        Assert.AreEqual(ConsentState.Idle, _flow.CurrentState);
    }

    private static ConsentNetMessage CreateMessage(
        ConsentNetMessageType type,
        string sessionId,
        ulong messageId,
        ulong senderClientId)
    {
        return new ConsentNetMessage
        {
            Type = type,
            SessionId = sessionId,
            MessageId = messageId,
            SenderClientId = senderClientId,
            Reason = string.Empty,
            SentAt = 0d
        };
    }

    private void InvokeReceive(ConsentNetMessage msg)
    {
        typeof(NgoNetworkAdapter)
            .GetMethod("ReceiveMessageClientRpc", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.Invoke(_adapter, new object[] { msg, default(ClientRpcParams) });
    }
}
