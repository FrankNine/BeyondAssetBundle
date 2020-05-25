using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
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
    public Matrix4x4[] bindpose;
    public Rect textureRect;
    public Vector2 textureRectOffset;
    public Vector2 atlasRectOffset;
    public UInt32 spriteSettingsRaw;
    public Vector4 uvTransform;
    public float downscaleMultiplier;
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
    public Vector3 Center;
    public Vector3 Extent;
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

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct Matrix4x4
{
    public float M00;
    public float M10;
    public float M20;
    public float M30;

    public float M01;
    public float M11;
    public float M21;
    public float M31;

    public float M02;
    public float M12;
    public float M22;
    public float M32;

    public float M03;
    public float M13;
    public float M23;
    public float M33;
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
            endiannessWriter.WriteRect(rect);

            var offset = new Vector2
            {
                X = 0,
                Y = 0
            };
            endiannessWriter.WriteVector2(offset);

            var border = new Vector4
            {
                X = 0,
                Y = 0,
                Z = 0,
                W = 0.609523833f
            };
            endiannessWriter.WriteVector4(border);

            float pixelToUnit = 10.1587305f;
            endiannessWriter.WriteSingle(pixelToUnit);

            var pivot = new Vector2
            {
                X = 0.5f,
                Y = 0.5f
            };
            endiannessWriter.WriteVector2(pivot);

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
            endiannessWriter.WritePPtr(spriteAtlas, serializedFileHeader.m_Version);

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
                            Center = new Vector3 {X = 0, Y = 0, Z = 0},
                            Extent = new Vector3 {X = 0, Y = 0, Z = 0}
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
                },
                bindpose = new Matrix4x4[0],
                textureRect = new Rect { X = 0, Y = 0, Width = 32, Height = 32 },
                textureRectOffset = new Vector2 { X = 0, Y = 0 },
                atlasRectOffset = new Vector2 { X = -1, Y = -1 },
                spriteSettingsRaw = 64,
                uvTransform = new Vector4 { X = 10.1587305f, Y = 16, Z = 10.1587305f, W = 16 },
                downscaleMultiplier = 1
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
                endiannessWriter.WriteAABB(subMesh.localAABB);
            }

            endiannessWriter.WriteInt32(spriteRenderData.m_IndexBuffer.Length);
            endiannessWriter.Write(spriteRenderData.m_IndexBuffer);
            
            endiannessWriter.WriteUInt32(spriteRenderData.m_VertexData.m_VertexCount);
            endiannessWriter.WriteInt32(spriteRenderData.m_VertexData.m_Channels.Length);
            foreach (var channel in spriteRenderData.m_VertexData.m_Channels)
            {
                endiannessWriter.Write(channel.stream);
                endiannessWriter.Write(channel.offset);
                endiannessWriter.Write(channel.format);
                endiannessWriter.Write(channel.dimension);
            }

            endiannessWriter.WriteInt32(spriteRenderData.m_DataSize.Length);
            endiannessWriter.Write(spriteRenderData.m_DataSize);
            
            endiannessWriter.WriteMatrixArray(spriteRenderData.bindpose);
            endiannessWriter.WriteRect(spriteRenderData.textureRect);
            endiannessWriter.WriteVector2(spriteRenderData.textureRectOffset);
            endiannessWriter.WriteVector2(spriteRenderData.atlasRectOffset);
            endiannessWriter.WriteUInt32(spriteRenderData.spriteSettingsRaw);
            endiannessWriter.WriteVector4(spriteRenderData.uvTransform);
            endiannessWriter.WriteSingle(spriteRenderData.downscaleMultiplier);

            Vector2[][] physicsShapeSize = new Vector2[][]
            {
                new Vector2[]
                {
                    new Vector2 {X = -1.57499993f, Y = 1.57499993f},
                    new Vector2 {X = -1.57499993f, Y = -1.57499993f},
                    new Vector2 {X = 1.57499993f, Y = -1.57499993f},
                    new Vector2 {X = 1.57499993f, Y = 1.57499993f},
                }
            };

            endiannessWriter.WriteInt32(physicsShapeSize.Length);
            foreach (var p in physicsShapeSize)
            {
                endiannessWriter.WriteVector2Array(p);
            }


            // TODO
            endiannessWriter.WriteUInt32(0);

            // AssetBundle
            endiannessWriter.WriteAlignedString("texture");

            // TODO
            endiannessWriter.Write(new[] {(byte) 0});

            var assetBundle = new AssetBundle
            {
                PreloadTable = new[]
                {
                    new PPtr {m_FileID = 0, m_PathID = -6905533648910529366},
                    new PPtr {m_FileID = 0, m_PathID = 6597701691304967057}
                },
                Container = new KeyValuePair<string, AssetInfo>[]
                {
                    new KeyValuePair<string, AssetInfo>
                    (
                        "assets/turtle.jpg", 
                        new AssetInfo
                        {
                            preloadIndex = 0,
                            preloadSize = 2,
                            asset = new PPtr
                            {
                                m_FileID = 0,
                                m_PathID = 6597701691304967057
                            }
                        }
                    ),
                    new KeyValuePair<string, AssetInfo>
                    (
                        "assets/turtle.jpg",
                        new AssetInfo
                        {
                            preloadIndex = 0,
                            preloadSize = 2,
                            asset = new PPtr
                            {
                                m_FileID = 0,
                                m_PathID = -6905533648910529366
                            }
                        }
                    ),
                }
            };
            endiannessWriter.WritePPtrArray(assetBundle.PreloadTable, serializedFileHeader.m_Version);
            endiannessWriter.WriteInt32(assetBundle.Container.Length);
            foreach (var c in assetBundle.Container)
            {
                endiannessWriter.WriteAlignedString(c.Key);
                // TODO
                endiannessWriter.Write(new byte[] { 0, 0, 0 });
                endiannessWriter.WriteAssetInfo(c.Value, serializedFileHeader.m_Version);
            }

            // TODO
            endiannessWriter.WriteWithoutEndianness
            (
                new byte[]
                {
                    00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00,
                    00, 00, 00, 00, 00, 00, 0x01, 00, 00, 00, 0x07, 00, 00, 00, 0x74, 0x65,
                    0x78, 0x74, 0x75, 0x72, 0x65, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00,
                    00, 00, 0x07, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00, 00
                }
            );

            var texture2D = new Texture2D
            {
                Name = "Turtle",

                m_ForcedFallbackFormat = 4,
                m_DownscaleFallback = false,

                Width = 32,
                Height = 32,

                textureFormat  = TextureFormat.RGB24,

                MipCount = 1,
                IsReadable = false,
                IsReadAllowed = false,
                StreamingMipmapsPriority = 0,
                ImageCount = 1,
                TextureDimension = 2,
                textureSettings = new GLTextureSettings
                {
                    m_FilterMode = 1,
                    m_Aniso = 1,
                    m_MipBias = 0,
                    m_WrapMode = 1,
                    m_WrapV = 1,
                    m_WrapW = 0
                },
                m_LightmapFormat = 6,
                m_ColorSpace = 1,
                image_data_size = 0,
                m_StreamData = new StreamingInfo
                {
                    offset = 0,
                    size = 3072,
                    path = "archive:/CAB-f04fab77212e693fb63bdad7458f66fe/CAB-f04fab77212e693fb63bdad7458f66fe.resS"
                }
            };

            endiannessWriter.WriteAlignedString(texture2D.Name);

            // TODO
            endiannessWriter.Write(new byte[2] { 0, 0 });

            endiannessWriter.WriteInt32(texture2D.m_ForcedFallbackFormat);
            endiannessWriter.WriteBoolean(texture2D.m_DownscaleFallback);

            // TODO
            endiannessWriter.Write(new byte[3] { 0, 0, 0 });

            endiannessWriter.WriteInt32(texture2D.Width);
            endiannessWriter.WriteInt32(texture2D.Height);
            endiannessWriter.WriteInt32(3072);
            endiannessWriter.WriteInt32((Int32)texture2D.textureFormat);
            endiannessWriter.WriteInt32(texture2D.MipCount);
            endiannessWriter.WriteBoolean(texture2D.IsReadable);
            endiannessWriter.WriteBoolean(texture2D.IsReadAllowed);
            //endiannessWriter.Align();
            // TODO
            endiannessWriter.Write(new byte[2] { 0, 0 });

            endiannessWriter.WriteInt32(texture2D.StreamingMipmapsPriority);
            endiannessWriter.WriteInt32(texture2D.ImageCount);
            endiannessWriter.WriteInt32(texture2D.TextureDimension);

            endiannessWriter.WriteInt32(texture2D.textureSettings.m_FilterMode);
            endiannessWriter.WriteInt32(texture2D.textureSettings.m_Aniso);
            endiannessWriter.WriteSingle(texture2D.textureSettings.m_MipBias);
            endiannessWriter.WriteInt32(texture2D.textureSettings.m_WrapMode);
            endiannessWriter.WriteInt32(texture2D.textureSettings.m_WrapV);
            endiannessWriter.WriteInt32(texture2D.textureSettings.m_WrapW);

            endiannessWriter.WriteInt32(texture2D.m_LightmapFormat);
            endiannessWriter.WriteInt32(texture2D.m_ColorSpace);
            endiannessWriter.WriteInt32(texture2D.image_data_size);

            endiannessWriter.WriteUInt32(texture2D.m_StreamData.offset);
            endiannessWriter.WriteUInt32(texture2D.m_StreamData.size);
            endiannessWriter.WriteAlignedString(texture2D.m_StreamData.path);

            // TODO
            endiannessWriter.Write(new byte[1] { 0});
            binaryWriter.Write(AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>("Assets/Turtle.jpg").GetRawTextureData());
        }
    }


}

class AssetBundle
{
    public PPtr[] PreloadTable;
    public KeyValuePair<string, AssetInfo>[] Container;
}

class AssetInfo
{
    public int preloadIndex;
    public int preloadSize;
    public PPtr asset;
}

class Texture2D
{
    public string Name;

    public int m_ForcedFallbackFormat;
    public bool m_DownscaleFallback;

    public int Width;
    public int Height;
    public TextureFormat textureFormat;
    public int MipCount;
    public bool IsReadable;
    public bool IsReadAllowed;
    public int StreamingMipmapsPriority;
    public int ImageCount;
    public int TextureDimension;
    public GLTextureSettings textureSettings;
    public int m_LightmapFormat;
    public int m_ColorSpace;
    public int image_data_size;
    public StreamingInfo m_StreamData;
}

public class GLTextureSettings
{
    public int m_FilterMode;
    public int m_Aniso;
    public float m_MipBias;
    public int m_WrapMode;
    public int m_WrapV;
    public int m_WrapW;
}

public class StreamingInfo
{
    public uint offset;
    public uint size;
    public string path;
}

enum TextureFormat
{
    Alpha8 = 1,
    ARGB4444,
    RGB24,
    RGBA32,
    ARGB32,
    RGB565 = 7,
    R16    = 9,
    DXT1,
    DXT5 = 12,
    RGBA4444,
    BGRA32,
    RHalf,
    RGHalf,
    RGBAHalf,
    RFloat,
    RGFloat,
    RGBAFloat,
    YUY2,
    RGB9e5Float,
    BC4 = 26,
    BC5,
    BC6H = 24,
    BC7,
    DXT1Crunched = 28,
    DXT5Crunched,
    PVRTC_RGB2,
    PVRTC_RGBA2,
    PVRTC_RGB4,
    PVRTC_RGBA4,
    ETC_RGB4,
    ATC_RGB4,
    ATC_RGBA8,
    EAC_R = 41,
    EAC_R_SIGNED,
    EAC_RG,
    EAC_RG_SIGNED,
    ETC2_RGB,
    ETC2_RGBA1,
    ETC2_RGBA8,
    ASTC_RGB_4x4,
    ASTC_RGB_5x5,
    ASTC_RGB_6x6,
    ASTC_RGB_8x8,
    ASTC_RGB_10x10,
    ASTC_RGB_12x12,
    ASTC_RGBA_4x4,
    ASTC_RGBA_5x5,
    ASTC_RGBA_6x6,
    ASTC_RGBA_8x8,
    ASTC_RGBA_10x10,
    ASTC_RGBA_12x12,
    ETC_RGB4_3DS,
    ETC_RGBA8_3DS,
    RG16,
    R8,
    ETC_RGB4Crunched,
    ETC2_RGBA8Crunched,
    ASTC_HDR_4x4,
    ASTC_HDR_5x5,
    ASTC_HDR_6x6,
    ASTC_HDR_8x8,
    ASTC_HDR_10x10,
    ASTC_HDR_12x12,
}