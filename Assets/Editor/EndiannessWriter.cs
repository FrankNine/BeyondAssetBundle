using System;
using System.IO;
using System.Text;

class EndiannessWriter : IDisposable
{
    private readonly byte[] _zeroTerminate = { 0 };

    private BinaryWriter _writer;
    private readonly Endianness _endianness;

    public EndiannessWriter(BinaryWriter writer, Endianness endianness)
    {
        _writer = writer;
        _endianness = endianness;
    }

    public void WriteWithoutEndianness(byte[] buffer)
        => _writer.Write(buffer);

    public void Write(byte[] buffer)
    {
        if (_endianness == Endianness.Little)
            Array.Reverse(buffer);

        WriteWithoutEndianness(buffer);
    }

    public void WriteInt32(Int32 value)
        => Write(BitConverter.GetBytes(value));

    public void WriteInt64(Int64 value)
        => Write(BitConverter.GetBytes(value));

    public void WriteUInt32(UInt32 value)
        => Write(BitConverter.GetBytes(value));

    public void WriteUInt64(UInt64 value)
        => Write(BitConverter.GetBytes(value));

    public void WriteString(string value)
    {
        WriteWithoutEndianness(Encoding.UTF8.GetBytes(value));
        WriteWithoutEndianness(_zeroTerminate);
    }

    public void Dispose()
    {
        _writer?.Dispose();
        _writer = null;
    }
}