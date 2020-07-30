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

    internal void Write(EndiannessWriter writer)
    {
        writer.WriteUInt32(MetadataSize);
        writer.WriteUInt32((UInt32)FileSize);
        writer.WriteUInt32(Version);
        writer.WriteUInt32((UInt32)DataOffset);

        writer.Write(new[] { (byte)Endianness });
        // Writing endianness changes from here
        // What the actual fuck?
        writer.Endianness = Endianness;

        writer.Write(Reserved);
        writer.WriteString(UnityVersion);
        writer.WriteInt32((Int32)BuildTarget);
        writer.WriteBoolean(IsTypeTreeEnabled);

        writer.WriteInt32(SerializedTypes.Length);
        foreach (var serializedType in SerializedTypes)
        {
            writer.WriteInt32(serializedType.ClassID);
            writer.WriteBoolean(serializedType.IsStrippedType);
            writer.WriteInt16(serializedType.ScriptTypeIndex);
            if ((Version < 16 && serializedType.ClassID < 0) ||
                (Version >= 16 && serializedType.ClassID == 114))
            {
                writer.Write(serializedType.ScriptID); //Hash128
            }
            writer.Write(serializedType.OldTypeHash);
        }

        writer.WriteInt32(ObjectInfos.Length);
        foreach (var objectInfo in ObjectInfos)
        {
            writer.Align(4);
            writer.WriteInt64(objectInfo.PathID);
            writer.WriteInt32((Int32)objectInfo.ByteStart);
            writer.WriteUInt32(objectInfo.ByteSize);
            writer.WriteInt32(objectInfo.TypeID);
        }

        // Script
        int scriptCount = 0;
        writer.WriteInt32(scriptCount);

        // Externals
        int externalCount = 0;
        writer.WriteInt32(externalCount);

        string userInformation = "";
        writer.WriteString(userInformation);
    }
}