namespace ConsentProximity.Core
{
    public interface IConsentService
    {
        ConsentState State { get; }
        TerminationReason? LastTermination { get; }
        ParticipantId A { get; }
        ParticipantId B { get; }
        ParticipantId? CurrentRequester { get; }

        void SetInRange(bool inRange);
        bool RequestConsent(ParticipantId requester);
        bool Accept(ParticipantId accepter);
        bool Cancel(ParticipantId requester);
        bool Withdraw(ParticipantId who);
        void Tick();
    }
}
