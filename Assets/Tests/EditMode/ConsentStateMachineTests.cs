using NUnit.Framework;
using ConsentProximity.Core;
using ConsentProximity.StateMachine;

public class ConsentStateMachineTests
{
    private static ConsentStateMachine CreateMachine(FakeClock clock, FakeDistanceProvider distance, float maxRange = 2f, float timeout = 5f)
    {
        var config = new ConsentConfig
        {
            maxRangeMeters = maxRange,
            requestTimeoutSeconds = timeout
        };

        return new ConsentStateMachine(
            new ParticipantId("A"),
            new ParticipantId("B"),
            config,
            clock,
            distance);
    }

    [Test]
    public void Transitions_HappyPath_ToActive_ThenWithdraws()
    {
        var clock = new FakeClock();
        var distance = new FakeDistanceProvider { DistanceMeters = 1f };
        var machine = CreateMachine(clock, distance);

        machine.SetInRange(true);
        Assert.AreEqual(ConsentState.InRange, machine.State);

        Assert.IsTrue(machine.RequestConsent(new ParticipantId("A")));
        Assert.AreEqual(ConsentState.Requested, machine.State);

        Assert.IsTrue(machine.Accept(new ParticipantId("B")));
        Assert.AreEqual(ConsentState.Active, machine.State);

        Assert.IsTrue(machine.Withdraw(new ParticipantId("A")));
        Assert.AreEqual(ConsentState.Terminated, machine.State);
        Assert.AreEqual(TerminationReason.WithdrawnConsent, machine.LastTermination);
    }

    [Test]
    public void Withdraw_Terminates_RegardlessOfState()
    {
        var clock = new FakeClock();
        var distance = new FakeDistanceProvider { DistanceMeters = 1f };
        var machine = CreateMachine(clock, distance);

        Assert.IsTrue(machine.Withdraw(new ParticipantId("A")));
        Assert.AreEqual(ConsentState.Terminated, machine.State);
        Assert.AreEqual(TerminationReason.WithdrawnConsent, machine.LastTermination);
    }

    [Test]
    public void Request_TimesOut_AndTerminates()
    {
        var clock = new FakeClock();
        var distance = new FakeDistanceProvider { DistanceMeters = 1f };
        var machine = CreateMachine(clock, distance, timeout: 2f);

        machine.SetInRange(true);
        Assert.IsTrue(machine.RequestConsent(new ParticipantId("A")));

        clock.Advance(2.1f);
        machine.Tick();

        Assert.AreEqual(ConsentState.Terminated, machine.State);
        Assert.AreEqual(TerminationReason.Timeout, machine.LastTermination);
    }
}
