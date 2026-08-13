using System;

namespace Deucarian.ActivityVisualization
{
    /// <summary>
    /// Identifies one indexed model element without imposing a backend identifier format.
    /// </summary>
    public readonly struct ModelElementId : IEquatable<ModelElementId>, IComparable<ModelElementId>
    {
        public ModelElementId(string scheme, string value)
        {
            if (string.IsNullOrWhiteSpace(scheme))
            {
                throw new ArgumentException("An identifier scheme is required.", nameof(scheme));
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("An identifier value is required.", nameof(value));
            }

            Scheme = scheme.Trim();
            Value = value.Trim();
        }

        public string Scheme { get; }

        public string Value { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Scheme) && !string.IsNullOrWhiteSpace(Value);

        public int CompareTo(ModelElementId other)
        {
            int schemeComparison = string.Compare(Scheme, other.Scheme, StringComparison.Ordinal);
            return schemeComparison != 0
                ? schemeComparison
                : string.Compare(Value, other.Value, StringComparison.Ordinal);
        }

        public bool Equals(ModelElementId other)
        {
            return string.Equals(Scheme, other.Scheme, StringComparison.Ordinal)
                && string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ModelElementId other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Scheme != null ? StringComparer.Ordinal.GetHashCode(Scheme) : 0) * 397)
                    ^ (Value != null ? StringComparer.Ordinal.GetHashCode(Value) : 0);
            }
        }

        public override string ToString()
        {
            return IsValid ? Scheme + ":" + Value : "<invalid>";
        }

        public static bool operator ==(ModelElementId left, ModelElementId right) => left.Equals(right);

        public static bool operator !=(ModelElementId left, ModelElementId right) => !left.Equals(right);
    }
}
