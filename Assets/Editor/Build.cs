using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using UnityEditor;

using K4os.Compression.LZ4;
using YamlDotNet.RepresentationModel;

public enum Endianness
{
    Little = 0,
    Big    = 1
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

    private static Int64 _GetPathId(string guid, Int64 fileId)
    {
        var input = new List<byte>();
        input.AddRange(Encoding.ASCII.GetBytes(guid));
        input.AddRange(BitConverter.GetBytes((Int32)3));
        input.AddRange(BitConverter.GetBytes(fileId));

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