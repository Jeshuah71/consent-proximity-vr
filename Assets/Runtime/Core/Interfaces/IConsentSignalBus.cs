using System;

namespace ConsentProximity.Core
{
    /// <summary>
    /// Template interface for sending and receiving consent signals.
    /// </summary>
    public interface IConsentSignalBus
    {
        event Action<ParticipantId> OnRequest;
        event Action<ParticipantId> OnAccept;
        event Action<ParticipantId> OnCancel;
        event Action<ParticipantId> OnWithdraw;

        void SendRequest(ParticipantId requester);
        void SendAccept(ParticipantId accepter);
        void SendCancel(ParticipantId requester);
        void SendWithdraw(ParticipantId who);
    }
}
