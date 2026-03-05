using NUnit.Framework;

public class SessionAuthorityRegistryTests
{
    [Test]
    public void FirstRequest_RegistersSession_AndIsAuthorized()
    {
        var registry = new SessionAuthorityRegistry();

        bool allowed = registry.IsAuthorized(
            ConsentNetMessageType.Request,
            "session-1",
            senderClientId: 10,
            targetClientId: 20);

        Assert.IsTrue(allowed);
    }

    [Test]
    public void Accept_MustComeFromResponder()
    {
        var registry = new SessionAuthorityRegistry();
        registry.IsAuthorized(ConsentNetMessageType.Request, "session-1", 10, 20);

        bool requesterAccept = registry.IsAuthorized(ConsentNetMessageType.Accept, "session-1", 10, 20);
        bool responderAccept = registry.IsAuthorized(ConsentNetMessageType.Accept, "session-1", 20, 10);

        Assert.IsFalse(requesterAccept);
        Assert.IsTrue(responderAccept);
    }

    [Test]
    public void Reject_MustComeFromResponder()
    {
        var registry = new SessionAuthorityRegistry();
        registry.IsAuthorized(ConsentNetMessageType.Request, "session-1", 10, 20);

        bool allowed = registry.IsAuthorized(ConsentNetMessageType.Reject, "session-1", 20, 10);
        Assert.IsTrue(allowed);
    }

    [Test]
    public void Withdraw_AllowsEitherParticipant_ToOtherSide()
    {
        var registry = new SessionAuthorityRegistry();
        registry.IsAuthorized(ConsentNetMessageType.Request, "session-1", 10, 20);

        bool requesterWithdraw = registry.IsAuthorized(ConsentNetMessageType.Withdraw, "session-1", 10, 20);
        bool responderWithdraw = registry.IsAuthorized(ConsentNetMessageType.Withdraw, "session-1", 20, 10);

        Assert.IsTrue(requesterWithdraw);
        Assert.IsTrue(responderWithdraw);
    }

    [Test]
    public void NonRequest_BeforeSessionExists_IsRejected()
    {
        var registry = new SessionAuthorityRegistry();

        bool allowed = registry.IsAuthorized(ConsentNetMessageType.Accept, "session-2", 30, 40);
        Assert.IsFalse(allowed);
    }

    [Test]
    public void SessionsRemoved_WhenClientDisconnects()
    {
        var registry = new SessionAuthorityRegistry();
        registry.IsAuthorized(ConsentNetMessageType.Request, "session-3", 100, 200);

        registry.RemoveSessionsForClient(100);

        // Session should be gone, so non-request message should now fail.
        bool allowed = registry.IsAuthorized(ConsentNetMessageType.Accept, "session-3", 200, 100);
        Assert.IsFalse(allowed);
    }
}
