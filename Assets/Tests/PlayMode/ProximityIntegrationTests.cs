using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using ConsentProximity.Core;
using ConsentProximity.StateMachine;

namespace ConsentProximity.Tests.PlayMode
{
    /// <summary>
    /// PlayMode integration tests for the ConsentStateMachine.
    /// Uses FakeClock and FakeDistanceProvider so no scene is needed.
    /// Run via: Window > General > Test Runner > PlayMode tab.
    /// </summary>
    public class ProximityIntegrationTests
    {
        private static ConsentStateMachine BuildMachine(
            FakeClock clock,
            FakeDistanceProvider distance,
            float maxRange = 2f,
            float timeout = 5f)
        {
            var config = new ConsentConfig
            {
                maxRangeMeters = maxRange,
                requestTimeoutSeconds = timeout
            };
            return new ConsentStateMachine(
                new ParticipantId("A"),
                new ParticipantId("B"),
                config, clock, distance);
        }

        /// <summary>
        /// Core flow: B walks in, B requests, A accepts, session goes Active.
        /// </summary>
        [UnityTest]
        public IEnumerator HappyPath_BRequests_AAccepts_ReachesActive()
        {
            var clock = new FakeClock();
            var distance = new FakeDistanceProvider { DistanceMeters = 1f };
            var machine = BuildMachine(clock, distance);

            machine.SetInRange(true);
            Assert.AreEqual(ConsentState.InRange, machine.State);

            bool requested = machine.RequestConsent(new ParticipantId("B"));
            Assert.IsTrue(requested);
            Assert.AreEqual(ConsentState.Requested, machine.State);

            yield return null;

            bool accepted = machine.Accept(new ParticipantId("A"));
            Assert.IsTrue(accepted);
            Assert.AreEqual(ConsentState.Active, machine.State);
        }

        /// <summary>
        /// Walking away during Active must immediately terminate with DistanceExceeded.
        /// </summary>
        [UnityTest]
        public IEnumerator WalkingAway_DuringActive_Terminates()
        {
            var clock = new FakeClock();
            var distance = new FakeDistanceProvider { DistanceMeters = 1f };
            var machine = BuildMachine(clock, distance);

            machine.SetInRange(true);
            machine.RequestConsent(new ParticipantId("B"));
            machine.Accept(new ParticipantId("A"));
            Assert.AreEqual(ConsentState.Active, machine.State);

            yield return null;

            distance.DistanceMeters = 3f;
            machine.Tick();

            Assert.AreEqual(ConsentState.Terminated, machine.State);
            Assert.AreEqual(TerminationReason.DistanceExceeded, machine.LastTermination);
        }

        /// <summary>
        /// Walking away during Requested must also terminate.
        /// </summary>
        [UnityTest]
        public IEnumerator WalkingAway_DuringRequested_Terminates()
        {
            var clock = new FakeClock();
            var distance = new FakeDistanceProvider { DistanceMeters = 1f };
            var machine = BuildMachine(clock, distance);

            machine.SetInRange(true);
            machine.RequestConsent(new ParticipantId("B"));
            Assert.AreEqual(ConsentState.Requested, machine.State);

            yield return null;

            distance.DistanceMeters = 3f;
            machine.Tick();

            Assert.AreEqual(ConsentState.Terminated, machine.State);
            Assert.AreEqual(TerminationReason.DistanceExceeded, machine.LastTermination);
        }

        /// <summary>
        /// A request left unanswered must time out.
        /// </summary>
        [UnityTest]
        public IEnumerator Request_LeftUnanswered_TimesOut()
        {
            var clock = new FakeClock();
            var distance = new FakeDistanceProvider { DistanceMeters = 1f };
            var machine = BuildMachine(clock, distance, timeout: 2f);

            machine.SetInRange(true);
            machine.RequestConsent(new ParticipantId("B"));

            yield return null;

            clock.Advance(2.1f);
            machine.Tick();

            Assert.AreEqual(ConsentState.Terminated, machine.State);
            Assert.AreEqual(TerminationReason.Timeout, machine.LastTermination);
        }

        /// <summary>
        /// B withdrawing during Active terminates immediately.
        /// </summary>
        [UnityTest]
        public IEnumerator Withdraw_ByRequester_DuringActive_Terminates()
        {
            var clock = new FakeClock();
            var distance = new FakeDistanceProvider { DistanceMeters = 1f };
            var machine = BuildMachine(clock, distance);

            machine.SetInRange(true);
            machine.RequestConsent(new ParticipantId("B"));
            machine.Accept(new ParticipantId("A"));

            yield return null;

            machine.Withdraw(new ParticipantId("B"));

            Assert.AreEqual(ConsentState.Terminated, machine.State);
            Assert.AreEqual(TerminationReason.WithdrawnConsent, machine.LastTermination);
        }

        /// <summary>
        /// A withdrawing (blocking) during Active terminates immediately.
        /// </summary>
        [UnityTest]
        public IEnumerator Withdraw_ByHost_DuringActive_Terminates()
        {
            var clock = new FakeClock();
            var distance = new FakeDistanceProvider { DistanceMeters = 1f };
            var machine = BuildMachine(clock, distance);

            machine.SetInRange(true);
            machine.RequestConsent(new ParticipantId("B"));
            machine.Accept(new ParticipantId("A"));

            yield return null;

            machine.Withdraw(new ParticipantId("A"));

            Assert.AreEqual(ConsentState.Terminated, machine.State);
            Assert.AreEqual(TerminationReason.WithdrawnConsent, machine.LastTermination);
        }

        /// <summary>
        /// A duplicate request while one is pending must be rejected.
        /// </summary>
        [UnityTest]
        public IEnumerator DuplicateRequest_IsRejected_StateUnchanged()
        {
            var clock = new FakeClock();
            var distance = new FakeDistanceProvider { DistanceMeters = 1f };
            var machine = BuildMachine(clock, distance);

            machine.SetInRange(true);
            machine.RequestConsent(new ParticipantId("B"));
            Assert.AreEqual(ConsentState.Requested, machine.State);

            yield return null;

            bool result = machine.RequestConsent(new ParticipantId("B"));
            Assert.IsFalse(result);
            Assert.AreEqual(ConsentState.Requested, machine.State);
        }

        /// <summary>
        /// Machine correctly implements IConsentService interface.
        /// </summary>
        [UnityTest]
        public IEnumerator Machine_ImplementsIConsentService()
        {
            var clock = new FakeClock();
            var distance = new FakeDistanceProvider { DistanceMeters = 1f };
            var machine = BuildMachine(clock, distance);

            IConsentService service = machine;

            Assert.IsNotNull(service);
            Assert.AreEqual(ConsentState.Idle, service.State);
            Assert.AreEqual(new ParticipantId("A"), service.A);
            Assert.AreEqual(new ParticipantId("B"), service.B);

            yield return null;

            service.SetInRange(true);
            Assert.AreEqual(ConsentState.InRange, service.State);
        }
    }
}