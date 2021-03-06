﻿using System;
using System.Diagnostics;

namespace Dalion.HttpMessageSigning {
    /// <summary>
    /// Represents an entity that the server can use to look up the component they need to verify the signature.
    /// </summary>
    [DebuggerDisplay("{" + nameof(ToString) + "()}")]
    public struct KeyId : IEquatable<KeyId> {
        public KeyId(string value) {
            Value = value ?? string.Empty;
        }
        
        public string Value { get; }

        public static KeyId Empty { get; } = new KeyId(string.Empty);
        
        public bool Equals(KeyId other) {
            return (Value ?? string.Empty) == other.Value;
        }

        public override bool Equals(object obj) {
            return obj is KeyId other && Equals(other);
        }

        public override int GetHashCode() {
            return (Value ?? string.Empty).GetHashCode();
        }

        public static bool operator ==(KeyId left, KeyId right) {
            return left.Equals(right);
        }

        public static bool operator !=(KeyId left, KeyId right) {
            return !left.Equals(right);
        }

        public static bool TryParse(string value, out KeyId parsed) {
            parsed = new KeyId(value);
            return true;
        }
        
        public static implicit operator KeyId(string value) {
            if (!TryParse(value, out var keyId)) {
                throw new FormatException($"The specified value ({value ?? "[null]"}) is not a valid string representation of a key id.");
            }
            return keyId;
        }
        
        public static implicit operator string(KeyId id) {
            return id.ToString();
        }

        public override string ToString() {
            return Value ?? string.Empty;
        }
    }
}