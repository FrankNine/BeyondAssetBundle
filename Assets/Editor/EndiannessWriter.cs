using System;
using System.IO;
using System.Linq;
using System.Text;

class EndiannessWriter : IDisposable
{
    private readonly byte[] _zeroTerminate = { 0 };

    private BinaryWriter _writer;
    public Endianness Endianness;

    public EndiannessWriter(BinaryWriter writer, Endianness endianness)
    {
        _writer = writer;
        Endianness = endianness;
    } 

    public void WriteWithoutEndianness(byte[] buffer)
        => _writer.Write(buffer);

    public void Write(byte[] buffer)
    {
        if (Endianness == Endianness.Big)
            Array.Reverse(buffer);

        WriteWithoutEndianness(buffer);
    }

    public void WriteInt16(Int16 value)
        => Write(BitConverter.GetBytes(value));

    public void WriteInt32(Int32 value)
        => Write(BitConverter.GetBytes(value));

    public void WriteInt64(Int64 value)
        => Write(BitConverter.GetBytes(value));


    public void WriteUInt16(UInt16 value)
        => Write(BitConverter.GetBytes(value));

    public void WriteUInt32(UInt32 value)
        => Write(BitConverter.GetBytes(value));

    public void WriteUInt64(UInt64 value)
        => Write(BitConverter.GetBytes(value));


    public void WriteSingle(float value)
        => Write(BitConverter.GetBytes(value));

    public void WriteVector2(Vector2 value)
    {
        WriteSingle(value.X);
        WriteSingle(value.Y);
    }

    public void WriteVector3(Vector3 value)
    {
        WriteSingle(value.X);
        WriteSingle(value.Y);
        WriteSingle(value.Z);
    }

    public void WriteVector4(Vector4 value)
    {
        WriteSingle(value.X);
        WriteSingle(value.Y);
        WriteSingle(value.Z);
        WriteSingle(value.W);
    }

    public void WriteRect(Rect value)
    {
        WriteSingle(value.X);
        WriteSingle(value.Y);
        WriteSingle(value.Width);
        WriteSingle(value.Height);
    }


    public void Align(int alignment)
    {
        var mod = _writer.BaseStream.Position % alignment;
        var padding = alignment - mod;
        WriteWithoutEndianness(Enumerable.Repeat((byte)0, (int)padding).ToArray());
    }

    public void WriteString(string value)
    {
        WriteWithoutEndianness(Encoding.UTF8.GetBytes(value));
        WriteWithoutEndianness(_zeroTerminate);
    }

    public void WriteAlignedString(string value)
    {
        WriteInt32(value.Length);
        WriteWithoutEndianness(Encoding.UTF8.GetBytes(value));
        //Align(4);
    }

    public void WritePPtr(PPtr pptr, UInt32 version)
    {
        WriteInt32(pptr.m_FileID);

        if (version < 14)
            WriteInt32((Int32)pptr.m_PathID);
        else
            WriteInt64(pptr.m_PathID);
    }

    public void WriteBoolean(bool value)
        => Write(BitConverter.GetBytes(value));

    public void Dispose()
    {
        _writer?.Dispose();
        _writer = null;
    }
}