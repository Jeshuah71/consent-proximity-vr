using System.Collections.Generic;
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

    [Test]
    public void DuplicateRequest_IsRejected_WithoutTermination()
    {
        var clock = new FakeClock();
        var distance = new FakeDistanceProvider { DistanceMeters = 1f };
        var machine = CreateMachine(clock, distance);

        int terminatedCount = 0;
        machine.OnTerminated += _ => terminatedCount++;

        machine.SetInRange(true);
        Assert.IsTrue(machine.RequestConsent(new ParticipantId("A")));
        Assert.AreEqual(ConsentState.Requested, machine.State);

        Assert.IsFalse(machine.RequestConsent(new ParticipantId("A")));
        Assert.AreEqual(ConsentState.Requested, machine.State);
        Assert.IsNull(machine.LastTermination);
        Assert.AreEqual(0, terminatedCount);
    }

    [Test]
    public void InvalidParticipant_Actions_AreRejected()
    {
        var clock = new FakeClock();
        var distance = new FakeDistanceProvider { DistanceMeters = 1f };
        var machine = CreateMachine(clock, distance);

        var intruder = new ParticipantId("X");

        machine.SetInRange(true);

        Assert.IsFalse(machine.RequestConsent(intruder));

        Assert.IsTrue(machine.RequestConsent(new ParticipantId("A")));
        Assert.IsFalse(machine.Accept(intruder));
        Assert.IsFalse(machine.Cancel(intruder));
        Assert.IsFalse(machine.Withdraw(intruder));

        Assert.AreEqual(ConsentState.Requested, machine.State);
        Assert.IsNull(machine.LastTermination);
    }

    [Test]
    public void RepeatedWithdraw_OnlyFirstCallTerminates()
    {
        var clock = new FakeClock();
        var distance = new FakeDistanceProvider { DistanceMeters = 1f };
        var machine = CreateMachine(clock, distance);

        int terminatedCount = 0;
        machine.OnTerminated += _ => terminatedCount++;

        Assert.IsTrue(machine.Withdraw(new ParticipantId("A")));
        Assert.AreEqual(ConsentState.Terminated, machine.State);
        Assert.AreEqual(TerminationReason.WithdrawnConsent, machine.LastTermination);
        Assert.AreEqual(1, terminatedCount);

        Assert.IsFalse(machine.Withdraw(new ParticipantId("A")));
        Assert.AreEqual(ConsentState.Terminated, machine.State);
        Assert.AreEqual(TerminationReason.WithdrawnConsent, machine.LastTermination);
        Assert.AreEqual(1, terminatedCount);
    }

    [Test]
    public void Termination_EmitsStateChange_BeforeTerminatedEvent()
    {
        var clock = new FakeClock();
        var distance = new FakeDistanceProvider { DistanceMeters = 1f };
        var machine = CreateMachine(clock, distance, timeout: 1f);

        var events = new List<string>();
        machine.OnStateChanged += (from, to) => events.Add($"state:{from}->{to}");
        machine.OnTerminated += reason => events.Add($"terminated:{reason}");

        machine.SetInRange(true);
        Assert.IsTrue(machine.RequestConsent(new ParticipantId("A")));
        clock.Advance(1.1f);
        machine.Tick();

        CollectionAssert.AreEqual(
            new[]
            {
                "state:Idle->InRange",
                "state:InRange->Requested",
                "state:Requested->Terminated",
                "terminated:Timeout"
            },
            events);
    }

    [Test]
    public void TerminatedState_DoesNotRestart_OnRangeChanges()
    {
        var clock = new FakeClock();
        var distance = new FakeDistanceProvider { DistanceMeters = 1f };
        var machine = CreateMachine(clock, distance);

        Assert.IsTrue(machine.Withdraw(new ParticipantId("A")));
        Assert.AreEqual(ConsentState.Terminated, machine.State);

        machine.SetInRange(true);
        machine.SetInRange(false);

        Assert.AreEqual(ConsentState.Terminated, machine.State);
        Assert.AreEqual(TerminationReason.WithdrawnConsent, machine.LastTermination);
    }
}
