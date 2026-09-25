using System;
using UnityEngine;

namespace fefek5.Currency.Runtime
{
    /// <summary>A long kept XOR-masked in memory, so memory scanners can't find the balance by its value.</summary>
    [Serializable]
    public struct ProtectedLong
    {
        [SerializeField] private long _;

        [SerializeField] private byte _h;

        private const long XorKey = 2971215073L;

        private ProtectedLong(long value)
        {
            _ = value ^ XorKey;
            _h = GetHash(_);
        }

        public static implicit operator ProtectedLong(long value) => new(value);

        public static implicit operator long(ProtectedLong value) =>
            value._ == 0 && value._h == 0 || value._h != GetHash(value._) ? 0 : value._ ^ XorKey;

        public override string ToString() => ((long)this).ToString();

        private static byte GetHash(long value) => (byte)(255 - value % 256);
    }
}
