using System;

namespace ConsentProximity.Core
{
    /// <summary>
    /// Simple ID to identify participants. For now its just a string. Later it could be a ulong/Guid.
    /// </summary>
    public readonly struct ParticipantId : IEquatable<ParticipantId>
    {
        public string Value { get; }

        public ParticipantId(string value) => Value = value ?? string.Empty;

        public bool Equals(ParticipantId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ParticipantId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value;

        public static bool operator ==(ParticipantId a, ParticipantId b) => a.Equals(b);
        public static bool operator !=(ParticipantId a, ParticipantId b) => !a.Equals(b);
    }
}
