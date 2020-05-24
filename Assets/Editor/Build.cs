﻿using System;
using System.Collections.Generic;
using System.IO;

using UnityEditor;

using K4os.Compression.LZ4;

enum BuildTarget
{
    UnknownPlatform    = 3716,
    DashboardWidget    = 1,
    StandaloneOSX      = 2,
    StandaloneOSXPPC   = 3,
    StandaloneOSXIntel = 4,
    StandaloneWindows,
    WebPlayer,
    WebPlayerStreamed,
    Wii = 8,
    iOS = 9,
    PS3,
    XBOX360,
    Android             = 13,
    StandaloneGLESEmu   = 14,
    NaCl                = 16,
    StandaloneLinux     = 17,
    FlashPlayer         = 18,
    StandaloneWindows64 = 19,
    WebGL,
    WSAPlayer,
    StandaloneLinux64 = 24,
    StandaloneLinuxUniversal,
    WP8Player,
    StandaloneOSXIntel64,
    BlackBerry,
    Tizen,
    PSP2,
    PS4,
    PSM,
    XboxOne,
    SamsungTV,
    N3DS,
    WiiU,
    tvOS,
    Switch,
    NoTarget = -2
}

enum Endianness
{
    Little = 0,
    Big    = 1
}

enum Compression
{
    None  = 0,
    LZMA  = 1,
    LZ4   = 2,
    LZ4HC = 3
}

class PPtr
{
    public int  m_FileID;
    public long m_PathID;
}

class SpriteRenderData
{
    public PPtr texture;
    public PPtr alphaTexture;
    public SubMesh[] m_SubMeshes;
    public byte[] m_IndexBuffer;
    public VertexData m_VertexData;
    public byte[] m_DataSize;
}

public class VertexData
{
    public uint          m_CurrentChannels;
    public uint          m_VertexCount;
    public ChannelInfo[] m_Channels;
    public StreamInfo[]  m_Streams;
    public byte[]        m_DataSize;
}

public class ChannelInfo
{
    public byte stream;
    public byte offset;
    public byte format;
    public byte dimension;
}

public class StreamInfo
{
    public uint   channelMask;
    public uint   offset;
    public uint   stride;
    public uint   align;
    public byte   dividerOp;
    public ushort frequency;
}

public enum GfxPrimitiveType : int
{
    kPrimitiveTriangles     = 0,
    kPrimitiveTriangleStrip = 1,
    kPrimitiveQuads         = 2,
    kPrimitiveLines         = 3,
    kPrimitiveLineStrip     = 4,
    kPrimitivePoints        = 5,
};

public class AABB
{
    public Vector3 m_Center;
    public Vector3 m_Extent;
}

public class SubMesh
{
    public uint             firstByte;
    public uint             indexCount;
    public GfxPrimitiveType topology;
    public uint             triangleCount;
    public uint             baseVertex;
    public uint             firstVertex;
    public uint             vertexCount;
    public AABB             localAABB;
}


public class SerializedFileHeader
{
    public uint m_MetadataSize;
    public long m_FileSize;
    public uint m_Version;
    public long m_DataOffset;
    public byte m_Endianess;
    public byte[] m_Reserved;
}


public class StorageBlock
{
    public UInt32 CompressedSize;
    public UInt32 UncompressedSize;
    public UInt16 Flags;
}

public class Node
{
    public Int64 Offset;
    public Int64 Size;
    public UInt32 Flags;
    public string Path;
}

public class TypeTreeNode
{
    public string m_Type;
    public string m_Name;
    public int m_ByteSize;
    public int m_Index;
    public int m_IsArray; //m_TypeFlags
    public int m_Version;
    public int m_MetaFlag;
    public int m_Level;
    public uint m_TypeStrOffset;
    public uint m_NameStrOffset;
    public ulong m_RefTypeHash;
}

public class SerializedType
{
    public int classID;
    public bool m_IsStrippedType;
    public short m_ScriptTypeIndex = -1;
    public List<TypeTreeNode> m_Nodes;
    public byte[] m_ScriptID; //Hash128
    public byte[] m_OldTypeHash; //Hash128
    public int[] m_TypeDependencies;
}

public class ObjectInfo
{
    public long byteStart;
    public uint byteSize;
    public int typeID;
    public int classID;

    public long m_PathID;
    public SerializedType serializedType;
}

public class Rect
{
    public float X;
    public float Y;
    public float Width;
    public float Height;
}

public class Vector2
{
    public float X;
    public float Y;
}

public class Vector3
{
    public float X;
    public float Y;
    public float Z;
}

public class Vector4
{
    public float X;
    public float Y;
    public float Z;
    public float W;
}

public class Build  
{
    [MenuItem("AssetBundle/Build AssetBundle")]
    public static void BuildAssetBundleOptions()
    {
        BuildPipeline.BuildAssetBundles
        (
            "Output", 
            UnityEditor.BuildAssetBundleOptions.UncompressedAssetBundle | 
            UnityEditor.BuildAssetBundleOptions.DisableWriteTypeTree, 
            UnityEditor.BuildTarget.Android
        );
    }


    private const int kArchiveCompressionTypeMask = 0x3F;

    [MenuItem("AssetBundle/Build Counterfeit")]
    public static void BuildCounterfeit()
    {
        var fileEndianness = Endianness.Little;

        using (var fileStream = File.Create("Counterfeit/texture"))
        using (var binaryWriter = new BinaryWriter(fileStream))
        using (var endiannessWriter = new EndiannessWriter(binaryWriter, Endianness.Big))
        {
            // Signature
            string signature = "UnityFS";
            endiannessWriter.WriteString(signature);

            // Header
            Int32 versionHead = 6;
            endiannessWriter.WriteInt32(versionHead);

            string unityVersionHead = "5.x.x";
            endiannessWriter.WriteString(unityVersionHead);

            string unityRevision = "2018.4.14f1";
            endiannessWriter.WriteString(unityRevision);

            Int64 size = 8142;
            endiannessWriter.WriteInt64(size);

            Int32 compressedBlocksInfoSize = 84;
            endiannessWriter.WriteInt32(compressedBlocksInfoSize);

            Int32 uncompressedBlocksInfoSize = 153;
            endiannessWriter.WriteInt32(uncompressedBlocksInfoSize);

            Int32 flags = 67;
            endiannessWriter.WriteInt32(flags);

            // ReadBlocksInfoAndDirectory
            byte[] buffer = new byte[uncompressedBlocksInfoSize];
            using (var stream = new MemoryStream(buffer))
            using (var binaryWriter2 = new BinaryWriter(stream))
            using (var endiannessWriter2 = new EndiannessWriter(binaryWriter2, Endianness.Big))
            {
                byte[] uncompressedDataHash = new byte[16] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
                endiannessWriter2.Write(uncompressedDataHash);

                Int32 blocksInfoCount = 1;
                endiannessWriter2.WriteInt32(blocksInfoCount);

                var storageBlock = new StorageBlock
                {
                    CompressedSize = 8008,
                    UncompressedSize = 8008,
                    Flags = 64
                };

                endiannessWriter2.WriteUInt32(storageBlock.CompressedSize);
                endiannessWriter2.WriteUInt32(storageBlock.UncompressedSize);
                endiannessWriter2.WriteUInt16(storageBlock.Flags);

                Int32 nodeCount = 2;
                endiannessWriter2.WriteInt32(nodeCount);

                var cabNode = new Node
                {
                    Offset = 0,
                    Size = 4936,
                    Flags = 4,
                    Path = "CAB-f04fab77212e693fb63bdad7458f66fe"
                };

                endiannessWriter2.WriteInt64(cabNode.Offset);
                endiannessWriter2.WriteInt64(cabNode.Size);
                endiannessWriter2.WriteUInt32(cabNode.Flags);
                endiannessWriter2.WriteString(cabNode.Path);

                var cabResSNode = new Node
                {
                    Offset = 4936,
                    Size = 3072,
                    Flags = 0,
                    Path = "CAB-f04fab77212e693fb63bdad7458f66fe.resS"
                };

                endiannessWriter2.WriteInt64(cabResSNode.Offset);
                endiannessWriter2.WriteInt64(cabResSNode.Size);
                endiannessWriter2.WriteUInt32(cabResSNode.Flags);
                endiannessWriter2.WriteString(cabResSNode.Path);
            }

            byte[] compressedBuffer = new byte[compressedBlocksInfoSize];
            LZ4Codec.Encode(buffer, 0, uncompressedBlocksInfoSize, compressedBuffer, 0, compressedBlocksInfoSize,
                LZ4Level.L11_OPT);

            endiannessWriter.WriteWithoutEndianness(compressedBuffer);


            var serializedFileHeader = new SerializedFileHeader
            {
                m_MetadataSize = 165,
                m_FileSize = 4936,
                m_Version = 17,
                m_DataOffset = 4096,
                m_Endianess = (byte) fileEndianness,
                m_Reserved = new byte[] { 0, 0, 0 }
            };

            endiannessWriter.WriteUInt32(serializedFileHeader.m_MetadataSize);
            endiannessWriter.WriteUInt32((UInt32)serializedFileHeader.m_FileSize);
            endiannessWriter.WriteUInt32(serializedFileHeader.m_Version);
            endiannessWriter.WriteUInt32((UInt32)serializedFileHeader.m_DataOffset);
            endiannessWriter.Write(new[] { serializedFileHeader.m_Endianess });

            // Writing endianness changes from here
            // What the actual fuck?
            endiannessWriter.Endianness = fileEndianness;

            endiannessWriter.Write(serializedFileHeader.m_Reserved);

            string unityVersion = "2018.4.14f1\n2";
            endiannessWriter.WriteString(unityVersion);

            BuildTarget target = BuildTarget.Android;
            endiannessWriter.WriteInt32((Int32) target);

            bool isTypeTreeEnabled = false;
            endiannessWriter.WriteBoolean(isTypeTreeEnabled);


            var types = new List<SerializedType>
            {
                new SerializedType
                {
                    classID = 213,
                    m_IsStrippedType = false,
                    m_ScriptTypeIndex = -1,
                    m_OldTypeHash = new byte[] {49, 77, 237, 224, 113, 54, 56, 179, 250, 209, 98, 143, 99, 41, 220, 98}
                },
                new SerializedType
                {
                    classID = 142,
                    m_IsStrippedType = false,
                    m_ScriptTypeIndex = -1,
                    m_OldTypeHash = new byte[] {151, 218, 95, 70, 136, 228, 90, 87, 200, 180, 45, 79, 66, 73, 114, 151}
                },
                new SerializedType
                {
                    classID = 28,
                    m_IsStrippedType = false,
                    m_ScriptTypeIndex = -1,
                    m_OldTypeHash = new byte[] {238, 108, 64, 129, 125, 41, 81, 146, 156, 219, 79, 90, 96, 135, 79, 93}
                }
            };

            // Type
            Int32 typeCount = 3;
            endiannessWriter.WriteInt32(typeCount);

            foreach (var type in types)
            {
                endiannessWriter.WriteInt32(type.classID);
                endiannessWriter.WriteBoolean(type.m_IsStrippedType);
                endiannessWriter.WriteInt16(type.m_ScriptTypeIndex);
                if ((serializedFileHeader.m_Version < 16 && type.classID < 0) || 
                    (serializedFileHeader.m_Version >= 16 && type.classID == 114))
                {
                    endiannessWriter.Write(type.m_ScriptID); //Hash128
                }

                endiannessWriter.Write(type.m_OldTypeHash);
            }

            var bigIDEnabled = 0;

            // Object
            Int32 objectCount = 3;
            endiannessWriter.WriteInt32(objectCount);

            var objects = new List<ObjectInfo>
            {
                new ObjectInfo
                {
                    m_PathID = -6905533648910529366,
                    byteStart = 0,
                    byteSize = 456,
                    typeID = 0
                },
                new ObjectInfo
                {
                    m_PathID = 1,
                    byteStart = 456,
                    byteSize = 188,
                    typeID = 1
                },
                new ObjectInfo
                {
                    m_PathID = 6597701691304967057,
                    byteStart = 648,
                    byteSize = 192,
                    typeID = 2
                }
            };

            foreach (var objectI in objects)
            {
                //endiannessWriter.Align(4);
                endiannessWriter.WriteInt64(objectI.m_PathID);
                endiannessWriter.WriteInt32((Int32)objectI.byteStart);
                endiannessWriter.WriteUInt32(objectI.byteSize);
                endiannessWriter.WriteInt32(objectI.typeID);
            }

            // Script
            int scriptCount = 0;
            endiannessWriter.WriteInt32(scriptCount);

            // Externals
            int externalCount = 0;
            endiannessWriter.WriteInt32(externalCount);

            string userInformation = "";
            endiannessWriter.WriteString(userInformation);

            // TODO
            endiannessWriter.Align((int)serializedFileHeader.m_DataOffset + 134);
            endiannessWriter.WriteAlignedString("Turtle");

            // TODO
            endiannessWriter.Write(new byte[]{0,0});

            var rect = new Rect
            {
                X = 0,
                Y = 0,
                Width = 32,
                Height = 32
            };
            endiannessWriter.WriteSingle(rect.X);
            endiannessWriter.WriteSingle(rect.Y);
            endiannessWriter.WriteSingle(rect.Width);
            endiannessWriter.WriteSingle(rect.Height);

            var offset = new Vector2
            {
                X = 0,
                Y = 0
            };

            endiannessWriter.WriteSingle(offset.X);
            endiannessWriter.WriteSingle(offset.Y);

            var border = new Vector4
            {
                X = 0,
                Y = 0,
                Z = 0,
                W = 0.609523833f
            };

            endiannessWriter.WriteSingle(border.X);
            endiannessWriter.WriteSingle(border.Y);
            endiannessWriter.WriteSingle(border.Z);
            endiannessWriter.WriteSingle(border.W);

            float pixelToUnit = 10.1587305f;

            endiannessWriter.WriteSingle(pixelToUnit);

            var pivot = new Vector2
            {
                X = 0.5f,
                Y = 0.5f
            };

            endiannessWriter.WriteSingle(pivot.X);
            endiannessWriter.WriteSingle(pivot.Y);

            Int32 extrude = 1;

            endiannessWriter.WriteInt32(extrude);

            bool isPolygon = false;
            endiannessWriter.WriteBoolean(isPolygon);

            // TODO
            endiannessWriter.Write(new byte[]{0,0,0});

            var guid = new Guid("218f5bb1-734f-41c3-b342-9ddc7d3930fb");
            endiannessWriter.Write(guid.ToByteArray());

            Int64 second = 21300000;
            endiannessWriter.WriteInt64(second);

            string[] atlasTags = new string[0] { };
            endiannessWriter.WriteInt32(atlasTags.Length);

            foreach (var a in atlasTags)
            {
                endiannessWriter.WriteAlignedString(a);
            }

            var spriteAtlas = new PPtr
            {
                m_FileID = 0,
                m_PathID = 0
            };

            endiannessWriter.WriteInt32(spriteAtlas.m_FileID);
            endiannessWriter.WriteInt64(spriteAtlas.m_PathID);

            var spriteRenderData = new SpriteRenderData
            {
                texture = new PPtr
                {
                    m_FileID = 0,
                    m_PathID = 6597701691304967057
                },
                alphaTexture = new PPtr
                {
                    m_FileID = 0,
                    m_PathID = 0
                },
                m_SubMeshes = new SubMesh[]
                {
                    new SubMesh
                    {
                        firstByte = 0,
                        indexCount = 6,
                        topology = GfxPrimitiveType.kPrimitiveTriangles,
                        baseVertex = 0,
                        firstVertex = 0,
                        vertexCount = 4,
                        localAABB = new AABB
                        {
                            m_Center = new Vector3 {X = 0, Y = 0, Z = 0},
                            m_Extent = new Vector3 {X = 0, Y = 0, Z = 0}
                        }
                    }
                },
                m_IndexBuffer = new byte[] {3, 0, 0, 0, 1, 0, 2, 0, 1, 0, 0, 0},
                m_VertexData = new VertexData
                {
                    m_VertexCount = 4,
                    m_Channels = new ChannelInfo[14]
                    {
                        new ChannelInfo
                        {
                            stream = 0,
                            offset = 0,
                            format = 0,
                            dimension = 3
                        },
                        new ChannelInfo
                        {
                            stream = 0,
                            offset = 0,
                            format = 0,
                            dimension = 0
                        },
                        new ChannelInfo
                        {
                            stream = 0,
                            offset = 0,
                            format = 0,
                            dimension = 0
                        },
                        new ChannelInfo
                        {
                            stream = 0,
                            offset = 0,
                            format = 0,
                            dimension = 0
                        },
                        new ChannelInfo
                        {
                            stream = 1,
                            offset = 0,
                            format = 0,
                            dimension = 2
                        },
                        new ChannelInfo
                        {
                            stream = 0,
                            offset = 0,
                            format = 0,
                            dimension = 0
                        },
                        new ChannelInfo
                        {
                            stream = 0,
                            offset = 0,
                            format = 0,
                            dimension = 0
                        },
                        new ChannelInfo
                        {
                            stream = 0,
                            offset = 0,
                            format = 0,
                            dimension = 0
                        },
                        new ChannelInfo
                        {
                            stream = 0,
                            offset = 0,
                            format = 0,
                            dimension = 0
                        },
                        new ChannelInfo
                        {
                            stream = 0,
                            offset = 0,
                            format = 0,
                            dimension = 0
                        },
                        new ChannelInfo
                        {
                            stream = 0,
                            offset = 0,
                            format = 0,
                            dimension = 0
                        },
                        new ChannelInfo
                        {
                            stream = 0,
                            offset = 0,
                            format = 0,
                            dimension = 0
                        },
                        new ChannelInfo
                        {
                            stream = 0,
                            offset = 0,
                            format = 0,
                            dimension = 0
                        },
                        new ChannelInfo
                        {
                            stream = 0,
                            offset = 0,
                            format = 0,
                            dimension = 0
                        }
                    }
                },
                m_DataSize = new byte[80]
                {
                    153, 153, 201, 191, 153, 153, 201, 63, 0, 0, 0, 0, 153, 153, 201, 63, 153, 153, 201, 191, 0, 0, 0,
                    0, 153, 153, 201, 63, 153, 153, 201, 63, 0, 0, 0, 0, 153, 153, 201, 191, 153, 153, 201, 191, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
                }
            };

            endiannessWriter.WritePPtr(spriteRenderData.texture, serializedFileHeader.m_Version);
            endiannessWriter.WritePPtr(spriteRenderData.alphaTexture, serializedFileHeader.m_Version);

            endiannessWriter.WriteInt32(spriteRenderData.m_SubMeshes.Length);
            foreach (var subMesh in spriteRenderData.m_SubMeshes)
            {
                endiannessWriter.WriteUInt32(subMesh.firstByte);
                endiannessWriter.WriteUInt32(subMesh.indexCount);
                endiannessWriter.WriteInt32((Int32)subMesh.topology);
                endiannessWriter.WriteUInt32(subMesh.baseVertex);
                endiannessWriter.WriteUInt32(subMesh.firstVertex);
                endiannessWriter.WriteUInt32(subMesh.vertexCount);

            }
           
        }
    }
}