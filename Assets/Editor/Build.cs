using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;

using K4os.Compression.LZ4;

public enum BuildTarget
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

public enum Endianness
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
}

public class BlocksInfoAndDirectory
{
    public byte[] UncompressedDataHash = new byte[16] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
    public StorageBlock[] StorageBlocks;
    public Node[] Nodes;
}

public class StorageBlock
{
    public UInt32 CompressedSize;
    public UInt32 UncompressedSize;
    public UInt16 Flags;
}

public class Node
{
    public Int64  Offset;
    public Int64  Size;
    public UInt32 Flags;
    public string Path;
}


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

public class SerializedType
{
    public Int32 ClassID;
    public bool IsStrippedType;
    public Int16 ScriptTypeIndex = -1;
    public List<TypeTreeNode> Nodes;
    public byte[] ScriptID; //Hash128
    public byte[] OldTypeHash; //Hash128
    public Int32[] TypeDependencies;
   
}

public class TypeTreeNode
{
    // TODO
}

public class ObjectInfo
{
    public Int64 ByteStart;
    public UInt32 ByteSize;
    public Int32 TypeID;
    public Int32 ClassID;
    public Int64 PathID;
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


    private static void _WriteHeader(EndiannessWriter endiannessWriter, UnityHeader header)
    {
        endiannessWriter.WriteString(header.Signature);
        endiannessWriter.WriteInt32(header.Version);
        endiannessWriter.WriteString(header.UnityVersion);
        endiannessWriter.WriteString(header.UnityRevision);
        endiannessWriter.WriteInt64(header.Size);
        endiannessWriter.WriteInt32(header.CompressedBlocksInfoSize);
        endiannessWriter.WriteInt32(header.UncompressedBlocksInfoSize);
        endiannessWriter.WriteInt32(header.Flags);
    }

    private static void _WriteBlocksInfoAndDirectory
    (
        EndiannessWriter endiannessWriter, 
        BlocksInfoAndDirectory blocksInfoAndDirectory
    )
    {
        endiannessWriter.Write(blocksInfoAndDirectory.UncompressedDataHash);

        endiannessWriter.WriteInt32(blocksInfoAndDirectory.StorageBlocks.Length);
        foreach (var storageBlock in blocksInfoAndDirectory.StorageBlocks)
        {
            endiannessWriter.WriteUInt32(storageBlock.CompressedSize);
            endiannessWriter.WriteUInt32(storageBlock.UncompressedSize);
            endiannessWriter.WriteUInt16(storageBlock.Flags);
        }

        endiannessWriter.WriteInt32(blocksInfoAndDirectory.Nodes.Length);
        foreach (var node in blocksInfoAndDirectory.Nodes)
        {
            endiannessWriter.WriteInt64(node.Offset);
            endiannessWriter.WriteInt64(node.Size);
            endiannessWriter.WriteUInt32(node.Flags);
            endiannessWriter.WriteString(node.Path);
        }
    }

    private static void _WriteSerializedFileHeader
    (
        EndiannessWriter endiannessWriter,
        SerializedFileHeader serializedFileHeader
    )
    {
        endiannessWriter.WriteUInt32(serializedFileHeader.MetadataSize);
        endiannessWriter.WriteUInt32((UInt32)serializedFileHeader.FileSize);
        endiannessWriter.WriteUInt32(serializedFileHeader.Version);
        endiannessWriter.WriteUInt32((UInt32)serializedFileHeader.DataOffset);

        endiannessWriter.Write(new[] { (byte)serializedFileHeader.Endianness });
        // Writing endianness changes from here
        // What the actual fuck?
        endiannessWriter.Endianness = serializedFileHeader.Endianness;

        endiannessWriter.Write(serializedFileHeader.Reserved);
        endiannessWriter.WriteString(serializedFileHeader.UnityVersion);
        endiannessWriter.WriteInt32((Int32)serializedFileHeader.BuildTarget);
        endiannessWriter.WriteBoolean(serializedFileHeader.IsTypeTreeEnabled);

        endiannessWriter.WriteInt32(serializedFileHeader.SerializedTypes.Length);
        foreach (var serializedType in serializedFileHeader.SerializedTypes)
        {
            endiannessWriter.WriteInt32(serializedType.ClassID);
            endiannessWriter.WriteBoolean(serializedType.IsStrippedType);
            endiannessWriter.WriteInt16(serializedType.ScriptTypeIndex);
            if ((serializedFileHeader.Version < 16 && serializedType.ClassID < 0) ||
                (serializedFileHeader.Version >= 16 && serializedType.ClassID == 114))
            {
                endiannessWriter.Write(serializedType.ScriptID); //Hash128
            }
            endiannessWriter.Write(serializedType.OldTypeHash);
        }

        endiannessWriter.WriteInt32(serializedFileHeader.ObjectInfos.Length);
        foreach (var objectInfo in serializedFileHeader.ObjectInfos)
        {
            //endiannessWriter.Align(4);
            endiannessWriter.WriteInt64(objectInfo.PathID);
            endiannessWriter.WriteInt32((Int32)objectInfo.ByteStart);
            endiannessWriter.WriteUInt32(objectInfo.ByteSize);
            endiannessWriter.WriteInt32(objectInfo.TypeID);
        }

        // Script
        int scriptCount = 0;
        endiannessWriter.WriteInt32(scriptCount);

        // Externals
        int externalCount = 0;
        endiannessWriter.WriteInt32(externalCount);

        string userInformation = "";
        endiannessWriter.WriteString(userInformation);
    }

    [MenuItem("AssetBundle/Build Counterfeit")]
    public static void BuildCounterfeit()
    {
        using (var fileStream = File.Create("Counterfeit/texture"))
        using (var binaryWriter = new BinaryWriter(fileStream))
        using (var endiannessWriter = new EndiannessWriter(binaryWriter, Endianness.Big))
        {
            var header = new UnityHeader
            {
                Signature = "UnityFS",
                Version = 6,
                UnityVersion = "5.x.x",
                UnityRevision = "2018.4.20f1",
                Size = 302235,
                CompressedBlocksInfoSize = 85,
                UncompressedBlocksInfoSize = 153,
                Flags = 67
            };

            _WriteHeader(endiannessWriter, header);

            var blocksInfoAndDirectory = new BlocksInfoAndDirectory
            {
                UncompressedDataHash = new byte[16] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                StorageBlocks = new[]
                {
                    new StorageBlock
                    {
                        CompressedSize = 302100,
                        UncompressedSize = 302100,
                        Flags = 64
                    }
                },
                Nodes = new[]
                {
                    new Node
                    {
                        Offset = 0,
                        Size = 4424,
                        Flags = 4,
                        Path = "CAB-f04fab77212e693fb63bdad7458f66fe"
                    },
                    new Node
                    {
                        Offset = 4424,
                        Size = 297676,
                        Flags = 0,
                        Path = "CAB-f04fab77212e693fb63bdad7458f66fe.resS"
                    }
                }
            };

            // ReadBlocksInfoAndDirectory
            byte[] buffer = new byte[header.UncompressedBlocksInfoSize];
            using (var stream = new MemoryStream(buffer))
            using (var binaryWriter2 = new BinaryWriter(stream))
            using (var endiannessWriterCompressed = new EndiannessWriter(binaryWriter2, Endianness.Big))
            {
                _WriteBlocksInfoAndDirectory(endiannessWriterCompressed, blocksInfoAndDirectory);
            }

            byte[] compressedBuffer = new byte[header.CompressedBlocksInfoSize];
            LZ4Codec.Encode
            (
                buffer, 
                0, 
                header.UncompressedBlocksInfoSize, 
                compressedBuffer, 
                0, 
                header.CompressedBlocksInfoSize,
                LZ4Level.L11_OPT
            );

            endiannessWriter.WriteWithoutEndianness(compressedBuffer);


            var serializedFileHeader = new SerializedFileHeader
            {
                MetadataSize = 125,
                FileSize = 4424,
                Version = 17,
                DataOffset = 4096,
                Endianness = Endianness.Little,
                Reserved = new byte[] { 0, 0, 0 },
                UnityVersion = "2018.4.20f1\n2",
                BuildTarget = BuildTarget.Android,
                IsTypeTreeEnabled = false,
                SerializedTypes = new[]
                {
                    new SerializedType
                    {
                        ClassID = 142,
                        IsStrippedType = false,
                        ScriptTypeIndex = -1,
                        OldTypeHash = new byte[]
                            {151, 218, 95, 70, 136, 228, 90, 87, 200, 180, 45, 79, 66, 73, 114, 151}
                    },
                    new SerializedType
                    {
                        ClassID = 28,
                        IsStrippedType = false,
                        ScriptTypeIndex = -1,
                        OldTypeHash = new byte[]
                            {238, 108, 64, 129, 125, 41, 81, 146, 156, 219, 79, 90, 96, 135, 79, 93}
                    }
                },
                ObjectInfos = new[]
                {
                    new ObjectInfo
                    {
                        PathID = 1,
                        ByteStart = 456,
                        ByteSize = 188,
                        TypeID = 1
                    },
                    new ObjectInfo
                    {
                        PathID = 6597701691304967057,
                        ByteStart = 648,
                        ByteSize = 192,
                        TypeID = 2
                    }
                }
            };

            _WriteSerializedFileHeader(endiannessWriter, serializedFileHeader);


            // TODO
            endiannessWriter.Align((int)serializedFileHeader.DataOffset + 134);
         
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
            endiannessWriter.WritePPtrArray(assetBundle.PreloadTable, serializedFileHeader.Version);
            endiannessWriter.WriteInt32(assetBundle.Container.Length);
            foreach (var c in assetBundle.Container)
            {
                endiannessWriter.WriteAlignedString(c.Key);
                // TODO
                endiannessWriter.Write(new byte[] { 0, 0, 0 });
                endiannessWriter.WriteAssetInfo(c.Value, serializedFileHeader.Version);
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