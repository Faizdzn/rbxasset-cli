using System.Buffers.Binary;
using System.Text;

namespace Module.Parser
{
    public sealed class ByteReader
    {
        private readonly ReadOnlyMemory<byte> _buffer;
        private int _index;

        public ByteReader(byte[] buffer) : this(buffer.AsMemory()) { }

        public ByteReader(ReadOnlyMemory<byte> buffer)
        {
            _buffer = buffer;
            _index = 0;
        }

        private ReadOnlySpan<byte> Span => _buffer.Span;

        public int GetIndex() => _index;
        public void SetIndex(int n) => _index = n;
        public int GetRemaining() => Span.Length - _index;
        public int GetLength() => Span.Length;
        public void Jump(int n) => _index += n;

        public ReadOnlySpan<byte> Array(int n)
        {
            var result = Span.Slice(_index, n);
            _index += n;
            return result;
        }

        public bool Match(string match)
        {
            if (_index + match.Length > Span.Length) return false;
            for (int i = 0; i < match.Length; i++)
                if (match[i] != Span[_index + i]) return false;
            _index += match.Length;
            return true;
        }

        public bool Match(ReadOnlySpan<byte> match)
        {
            if (_index + match.Length > Span.Length) return false;
            if (!Span.Slice(_index, match.Length).SequenceEqual(match)) return false;
            _index += match.Length;
            return true;
        }

        public byte Byte() => Span[_index++];
        public byte UInt8() => Span[_index++];

        public ushort UInt16LE()
        {
            ushort v = BinaryPrimitives.ReadUInt16LittleEndian(Span.Slice(_index, 2));
            _index += 2; return v;
        }

        public ushort UInt16BE()
        {
            ushort v = BinaryPrimitives.ReadUInt16BigEndian(Span.Slice(_index, 2));
            _index += 2; return v;
        }

        public uint UInt32LE()
        {
            uint v = BinaryPrimitives.ReadUInt32LittleEndian(Span.Slice(_index, 4));
            _index += 4; return v;
        }

        public uint UInt32BE()
        {
            uint v = BinaryPrimitives.ReadUInt32BigEndian(Span.Slice(_index, 4));
            _index += 4; return v;
        }

        public ulong UInt64LE()
        {
            ulong v = BinaryPrimitives.ReadUInt64LittleEndian(Span.Slice(_index, 8));
            _index += 8; return v;
        }

        public ulong UInt64BE()
        {
            ulong v = BinaryPrimitives.ReadUInt64BigEndian(Span.Slice(_index, 8));
            _index += 8; return v;
        }

        public sbyte Int8() => unchecked((sbyte)Span[_index++]);

        public short Int16LE()
        {
            short v = BinaryPrimitives.ReadInt16LittleEndian(Span.Slice(_index, 2));
            _index += 2; return v;
        }

        public short Int16BE()
        {
            short v = BinaryPrimitives.ReadInt16BigEndian(Span.Slice(_index, 2));
            _index += 2; return v;
        }

        public int Int32LE()
        {
            int v = BinaryPrimitives.ReadInt32LittleEndian(Span.Slice(_index, 4));
            _index += 4; return v;
        }

        public int Int32BE()
        {
            int v = BinaryPrimitives.ReadInt32BigEndian(Span.Slice(_index, 4));
            _index += 4; return v;
        }

        public long Int64LE()
        {
            long v = BinaryPrimitives.ReadInt64LittleEndian(Span.Slice(_index, 8));
            _index += 8; return v;
        }

        public long Int64BE()
        {
            long v = BinaryPrimitives.ReadInt64BigEndian(Span.Slice(_index, 8));
            _index += 8; return v;
        }

        public float FloatLE()
        {
            int bits = BinaryPrimitives.ReadInt32LittleEndian(Span.Slice(_index, 4));
            _index += 4;
            return BitConverter.Int32BitsToSingle(bits);
        }

        public float FloatBE()
        {
            int bits = BinaryPrimitives.ReadInt32BigEndian(Span.Slice(_index, 4));
            _index += 4;
            return BitConverter.Int32BitsToSingle(bits);
        }

        public double DoubleLE()
        {
            long bits = BinaryPrimitives.ReadInt64LittleEndian(Span.Slice(_index, 8));
            _index += 8;
            return BitConverter.Int64BitsToDouble(bits);
        }

        public double DoubleBE()
        {
            long bits = BinaryPrimitives.ReadInt64BigEndian(Span.Slice(_index, 8));
            _index += 8;
            return BitConverter.Int64BitsToDouble(bits);
        }

        public string String(int n)
        {
            var bytes = Span.Slice(_index, n);
            _index += n;
            return Encoding.UTF8.GetString(bytes);
        }
    }

}