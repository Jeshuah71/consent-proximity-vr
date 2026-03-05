using System.Collections.Generic;

public sealed class SessionAuthorityRegistry
{
    private struct SessionParticipants
    {
        public ulong Requester;
        public ulong Responder;
    }

    private readonly Dictionary<string, SessionParticipants> _sessions =
        new Dictionary<string, SessionParticipants>();

    public bool IsAuthorized(ConsentNetMessageType type, string sessionId, ulong senderClientId, ulong targetClientId)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return false;
        if (senderClientId == targetClientId) return false;

        if (!_sessions.TryGetValue(sessionId, out var participants))
        {
            if (type != ConsentNetMessageType.Request) return false;

            _sessions[sessionId] = new SessionParticipants
            {
                Requester = senderClientId,
                Responder = targetClientId
            };
            return true;
        }

        bool senderIsRequester = senderClientId == participants.Requester;
        bool senderIsResponder = senderClientId == participants.Responder;

        if (!senderIsRequester && !senderIsResponder) return false;

        switch (type)
        {
            case ConsentNetMessageType.Request:
                return senderIsRequester && targetClientId == participants.Responder;
            case ConsentNetMessageType.Accept:
            case ConsentNetMessageType.Reject:
                return senderIsResponder && targetClientId == participants.Requester;
            case ConsentNetMessageType.Withdraw:
            case ConsentNetMessageType.Terminate:
                return targetClientId == (senderIsRequester ? participants.Responder : participants.Requester);
            default:
                return false;
        }
    }

    public void RemoveSessionsForClient(ulong clientId)
    {
        if (_sessions.Count == 0) return;

        var toRemove = new List<string>();
        foreach (var pair in _sessions)
        {
            if (pair.Value.Requester == clientId || pair.Value.Responder == clientId)
            {
                toRemove.Add(pair.Key);
            }
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            _sessions.Remove(toRemove[i]);
        }
    }
}
