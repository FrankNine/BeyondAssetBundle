using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using K4os.Compression.LZ4;
using YamlDotNet.RepresentationModel;

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
    public Int64 PathID;
    public Int64 ByteStart;
    public UInt32 ByteSize;
    public Int32 TypeID;
}


class AssetBundle
{
    public string Name;
    public PPtr[] PreloadTable;
    public KeyValuePair<string, AssetInfo>[] Container;
    public AssetInfo MainAsset;
    public UInt32 RuntimeCompatibility;
    public string AssetBundleName;
    public string[] DependencyAssetBundleNames;
    public bool IsStreamedSceneAssetBundle;
    public Int32 ExplicitDataLayout;
    public Int32 PathFlags;
    public Dictionary<string, string> SceneHashes;
}

class PPtr
{
    public Int32 FileID;
    public Int64 PathID;
}

class AssetInfo
{
    public Int32 PreloadIndex;
    public Int32 PreloadSize;
    public PPtr Asset;
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
            endiannessWriter.Align(4);
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


    private static void _WriteAssetBundle
    (
        EndiannessWriter endiannessWriter,
        AssetBundle assetBundle,
        UInt32 serializationVersion
    )
    {
        endiannessWriter.WriteAlignedString(assetBundle.Name);

        endiannessWriter.WritePPtrArray(assetBundle.PreloadTable, serializationVersion);
        endiannessWriter.WriteInt32(assetBundle.Container.Length);
        foreach (var container in assetBundle.Container)
        {
            endiannessWriter.WriteAlignedString(container.Key);
            endiannessWriter.WriteAssetInfo(container.Value, serializationVersion);
        }

        endiannessWriter.WriteAssetInfo(assetBundle.MainAsset, serializationVersion);

        endiannessWriter.WriteUInt32(assetBundle.RuntimeCompatibility);

        endiannessWriter.WriteAlignedString(assetBundle.AssetBundleName);
        endiannessWriter.WriteInt32(assetBundle.DependencyAssetBundleNames.Length);
        foreach (var dependencyAssetBundleName in assetBundle.DependencyAssetBundleNames)
        {
            endiannessWriter.WriteAlignedString(dependencyAssetBundleName);
        }
        
        endiannessWriter.WriteBoolean(assetBundle.IsStreamedSceneAssetBundle);
        endiannessWriter.Align(4);
        endiannessWriter.WriteInt32(assetBundle.ExplicitDataLayout);
        endiannessWriter.WriteInt32(assetBundle.PathFlags);

        endiannessWriter.WriteInt32(assetBundle.SceneHashes.Count);
        foreach (var sceneHash in assetBundle.SceneHashes)
        {
            endiannessWriter.WriteString(sceneHash.Key);
            endiannessWriter.WriteString(sceneHash.Value);
        }
    }

    private static void _WriteTexture2D
    (
        EndiannessWriter endiannessWriter,
        Texture2D texture2D
    )
    {
        endiannessWriter.WriteAlignedString(texture2D.Name);

        endiannessWriter.WriteInt32(texture2D.ForcedFallbackFormat);
        endiannessWriter.WriteBoolean(texture2D.DownscaleFallback);
        endiannessWriter.Align(4);

        endiannessWriter.WriteInt32(texture2D.Width);
        endiannessWriter.WriteInt32(texture2D.Height);
        endiannessWriter.WriteInt32(texture2D.CompleteImageSize);
        endiannessWriter.WriteInt32((Int32) texture2D.TextureFormat);
        endiannessWriter.WriteInt32(texture2D.MipCount);

        endiannessWriter.WriteBoolean(texture2D.IsReadable);
        endiannessWriter.WriteBoolean(texture2D.IsReadAllowed);
        endiannessWriter.Align(4);

        endiannessWriter.WriteInt32(texture2D.StreamingMipmapsPriority);
        endiannessWriter.WriteInt32(texture2D.ImageCount);
        endiannessWriter.WriteInt32(texture2D.TextureDimension);

        endiannessWriter.WriteInt32(texture2D.TextureSettings.FilterMode);
        endiannessWriter.WriteInt32(texture2D.TextureSettings.Aniso);
        endiannessWriter.WriteSingle(texture2D.TextureSettings.MipBias);
        endiannessWriter.WriteInt32(texture2D.TextureSettings.WrapMode);
        endiannessWriter.WriteInt32(texture2D.TextureSettings.WrapV);
        endiannessWriter.WriteInt32(texture2D.TextureSettings.WrapW);

        endiannessWriter.WriteInt32(texture2D.LightmapFormat);
        endiannessWriter.WriteInt32(texture2D.ColorSpace);
        endiannessWriter.WriteInt32(texture2D.ImageDataSize);

        endiannessWriter.WriteUInt32(texture2D.StreamData.Offset);
        endiannessWriter.WriteUInt32(texture2D.StreamData.Size);
        endiannessWriter.WriteAlignedString(texture2D.StreamData.Path);
    }

    private static YamlNode _FindYamlChildNode(YamlMappingNode node, string tag)
    {
        foreach (var entry in node.Children)
        {
            if(((YamlScalarNode)entry.Key).Value == tag)
            {
                return entry.Value;
            }
        }

        return null;
    }

    private static Int64 _GetPathId(string guid, int fileId)
    {
        var input = new List<byte>();
        input.AddRange(Encoding.ASCII.GetBytes(guid));
        input.AddRange(BitConverter.GetBytes((Int32)3));
        input.AddRange(BitConverter.GetBytes((Int64)fileId));

        var output = Md4.Md4Hash(input);
        return BitConverter.ToInt64(output.Take(8).ToArray(), 0);
    }

    public static string ByteArrayToString(byte[] ba)
    {
        return BitConverter.ToString(ba).Replace("-", "");
    }

    [MenuItem("AssetBundle/Build Counterfeit")]
    public static void BuildCounterfeit()
    {
        const string texturePath = "Assets/Turtle.jpg";
        const string textureMetaPath = texturePath + ".meta";

        var metaContent = new StringReader(File.ReadAllText(textureMetaPath));
        var yaml = new YamlStream();
        yaml.Load(metaContent);

        var rootNode = yaml.Documents[0].RootNode;
        var guidNode = _FindYamlChildNode((YamlMappingNode)rootNode, "guid");
        string guid = ((YamlScalarNode) guidNode).Value;
        var textureImporterNode = _FindYamlChildNode((YamlMappingNode)rootNode, "TextureImporter");
        var assetBundleNameNode = _FindYamlChildNode((YamlMappingNode)textureImporterNode, "assetBundleName");
        string assetBundleName = ((YamlScalarNode)assetBundleNameNode).Value;
        string cabFilename = "CAB-" + ByteArrayToString(Md4.Md4Hash(new List<byte>(Encoding.ASCII.GetBytes(assetBundleName)))).ToLower();
        string cabRessFilename = cabFilename + ".resS";
        string archivePath = $"archive:/{cabFilename}/{cabRessFilename}";

        // TODO: Replace jpeg loading library
        var unityTexture2D = AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(texturePath);
        byte[] textureRawData = unityTexture2D.GetRawTextureData();
        int width = unityTexture2D.width;
        int height = unityTexture2D.height;

        int textureFileId = 2800000;
        Int64 texturePathId = _GetPathId(guid, textureFileId);

        var blocksInfoAndDirectory = new BlocksInfoAndDirectory
        {
            UncompressedDataHash = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
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
                    Path = cabFilename
                },
                new Node
                {
                    Offset = 4424,
                    Size = 297676,
                    Flags = 0,
                    Path = cabRessFilename
                }
            }
        };

        byte[] compressedBuffer;
        int uncompressedSize;
        int compressedSize;

        using (var memoryStream = new MemoryStream())
        using (var memoryBinaryWriter = new BinaryWriter(memoryStream))
        using (var endiannessWriterCompressed = new EndiannessWriter(memoryBinaryWriter, Endianness.Big))
        {
            _WriteBlocksInfoAndDirectory(endiannessWriterCompressed, blocksInfoAndDirectory);

            byte[] uncompressedBuffer = memoryStream.ToArray();
            uncompressedSize = uncompressedBuffer.Length;
            // Assume compressed buffer always smaller than uncompressed
            compressedBuffer = new byte[uncompressedSize];

            compressedSize = LZ4Codec.Encode
            (
                uncompressedBuffer,
                0,
                uncompressedSize,
                compressedBuffer,
                0,
                uncompressedSize,
                LZ4Level.L11_OPT
            );

            compressedBuffer = compressedBuffer.Take(compressedSize).ToArray();
        }

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
                    // https://docs.unity3d.com/Manual/ClassIDReference.html
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
                    ByteStart = 0,
                    ByteSize = 132,
                    TypeID = 0
                },
                new ObjectInfo
                {
                    PathID = texturePathId,
                    ByteStart = 136,
                    ByteSize = 192,
                    TypeID = 1
                }
            }
        };

        var assetBundle = new AssetBundle
        {
            Name = assetBundleName,
            PreloadTable = new[]
               {
                    new PPtr {FileID = 0, PathID = texturePathId}
                },
            Container = new[]
               {
                    new KeyValuePair<string, AssetInfo>
                    (
                        texturePath.ToLower(),
                        new AssetInfo
                        {
                            PreloadIndex = 0,
                            PreloadSize = 1,
                            Asset = new PPtr
                            {
                                FileID = 0,
                                PathID = texturePathId
                            }
                        }
                    )
                },
            MainAsset = new AssetInfo
            {
                PreloadIndex = 0,
                PreloadSize = 0,
                Asset = new PPtr
                {
                    FileID = 0,
                    PathID = 0
                }
            },
            RuntimeCompatibility = 1,
            AssetBundleName = assetBundleName,
            DependencyAssetBundleNames = new string[0],
            IsStreamedSceneAssetBundle = false,
            ExplicitDataLayout = 0,
            PathFlags = 7,
            SceneHashes = new Dictionary<string, string>()
        };

        var texture2D = new Texture2D
        {
            Name = Path.GetFileNameWithoutExtension(texturePath),

            ForcedFallbackFormat = (int)TextureFormat.RGBA32,
            DownscaleFallback = false,

            Width = width,
            Height = height,
            CompleteImageSize = textureRawData.Length,

            TextureFormat = TextureFormat.RGB24,

            MipCount = 1,
            IsReadable = false,
            IsReadAllowed = false,
            StreamingMipmapsPriority = 0,
            ImageCount = 1,
            TextureDimension = 2,
            TextureSettings = new GLTextureSettings
            {
                FilterMode = 1,
                Aniso = 1,
                MipBias = 0,
                WrapMode = 0,
                WrapV = 0,
                WrapW = 0
            },
            LightmapFormat = 0,
            ColorSpace = 1,
            ImageDataSize = 0,
            StreamData = new StreamingInfo
            {
                Offset = 0,
                Size = (UInt32)textureRawData.Length,
                Path = archivePath
            }
        };

        byte[] serializeFileBuffer;
        int metadataSize;
        int assetBundleObjectPosition;
        int assetBundleObjectOffset;
        int assetBundleObjectSize;
        int texture2DPosition;
        int texture2DOffset;
        int texture2DObjectSize;

        using (var memoryStream = new MemoryStream())
        using (var memoryBinaryWriter = new BinaryWriter(memoryStream))
        using (var endiannessWriterStorage = new EndiannessWriter(memoryBinaryWriter, Endianness.Big))
        {
            _WriteSerializedFileHeader(endiannessWriterStorage, serializedFileHeader);
            metadataSize = (Int32)endiannessWriterStorage.Position;

            endiannessWriterStorage.Align((int)serializedFileHeader.DataOffset);
            assetBundleObjectPosition = (Int32)endiannessWriterStorage.Position;
            assetBundleObjectOffset = (Int32)(assetBundleObjectPosition - serializedFileHeader.DataOffset);
            _WriteAssetBundle(endiannessWriterStorage, assetBundle, serializedFileHeader.Version);
            assetBundleObjectSize = (Int32)(endiannessWriterStorage.Position - assetBundleObjectPosition);

            // TODO: What is this padding?
            endiannessWriterStorage.WriteUInt32(0);

            texture2DPosition = (Int32)endiannessWriterStorage.Position;
            texture2DOffset = (Int32)(texture2DPosition - serializedFileHeader.DataOffset);
            _WriteTexture2D(endiannessWriterStorage, texture2D);
            texture2DObjectSize = (Int32) (endiannessWriterStorage.Position - texture2DPosition);

            endiannessWriterStorage.WriteWithoutEndianness(textureRawData);

            // TODO: What is this padding?
            endiannessWriterStorage.Write(0);

            serializeFileBuffer = memoryStream.ToArray();
        }

        var header = new UnityHeader
        {
            Signature = "UnityFS",
            Version = 6,
            UnityVersion = "5.x.x",
            UnityRevision = "2018.4.20f1",
            Size = 302235,
            CompressedBlocksInfoSize = compressedSize,
            UncompressedBlocksInfoSize = uncompressedSize,
            Flags = 67
        };

        using (var fileStream = File.Create("Counterfeit/texture"))
        using (var binaryWriter = new BinaryWriter(fileStream))
        using (var endiannessWriter = new EndiannessWriter(binaryWriter, Endianness.Big))
        {
            _WriteHeader(endiannessWriter, header);
            endiannessWriter.WriteWithoutEndianness(compressedBuffer);
            endiannessWriter.WriteWithoutEndianness(serializeFileBuffer);
        }
    }
}



class Texture2D
{
    public string Name;

    // Inherit from Texture
    public Int32 ForcedFallbackFormat;
    public bool DownscaleFallback;

    public Int32 Width;
    public Int32 Height;
    public Int32 CompleteImageSize;

    public TextureFormat TextureFormat;

    public Int32 MipCount;

    public bool IsReadable;
    public bool IsReadAllowed;

    public int StreamingMipmapsPriority;
    public int ImageCount;
    public int TextureDimension;
    public GLTextureSettings TextureSettings;
    public int LightmapFormat;
    public int ColorSpace;
    public int ImageDataSize;
    public StreamingInfo StreamData;
}

public class GLTextureSettings
{
    public Int32 FilterMode;
    public Int32 Aniso;
    public float MipBias;
    public Int32 WrapMode;
    public Int32 WrapV;
    public Int32 WrapW;
}

public class StreamingInfo
{
    public uint Offset;
    public uint Size;
    public string Path;
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