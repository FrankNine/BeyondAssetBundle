using System;

public class UnityHeader
{
    public string Signature;
    public Int32 Version;
    public string UnityVersion;
    public string UnityRevision;
    public Int64 Size;
    public Int32 CompressedBlocksInfoSize;
    public Int32 UncompressedBlocksInfoSize;
    public Int32 Flags;

    internal void Write(EndiannessWriter writer)
    {
        writer.WriteString(Signature);
        writer.WriteInt32(Version);
        writer.WriteString(UnityVersion);
        writer.WriteString(UnityRevision);
        writer.WriteInt64(Size);
        writer.WriteInt32(CompressedBlocksInfoSize);
        writer.WriteInt32(UncompressedBlocksInfoSize);
        writer.WriteInt32(Flags);
    }
}