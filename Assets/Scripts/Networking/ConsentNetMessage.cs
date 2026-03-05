using Unity.Collections;
using Unity.Netcode;

public enum ConsentNetMessageType : byte
{
    Request,
    Accept,
    Reject,
    Withdraw,
    Terminate
}

public struct ConsentNetMessage : INetworkSerializable
{
    public ConsentNetMessageType Type;
    public FixedString64Bytes SessionId;
    public ulong MessageId;
    public ulong SenderClientId;
    public FixedString64Bytes Reason;
    public double SentAt;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Type);
        serializer.SerializeValue(ref SessionId);
        serializer.SerializeValue(ref MessageId);
        serializer.SerializeValue(ref SenderClientId);
        serializer.SerializeValue(ref Reason);
        serializer.SerializeValue(ref SentAt);
    }
}
