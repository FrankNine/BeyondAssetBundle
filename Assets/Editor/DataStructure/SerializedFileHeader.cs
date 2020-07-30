using System;

public class SerializedFileHeader
{
    public UInt32 MetadataSize;
    public Int64 FileSize;
    public UInt32 Version;
    public Int64 DataOffset;
    public Endianness Endianness;
    public byte[] Reserved;
    public string UnityVersion;
    public BuildTarget BuildTarget;
    public bool IsTypeTreeEnabled;
    public SerializedType[] SerializedTypes;
    public ObjectInfo[] ObjectInfos;
}