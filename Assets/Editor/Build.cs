using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using UnityEditor;

using K4os.Compression.LZ4;
using YamlDotNet.RepresentationModel;

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

    [MenuItem("AssetBundle/Build Counterfeit")]
    public static void BuildCounterfeit()
    {
        const string texturePath = "Assets/Turtle.jpg";
        const string textureMetaPath = texturePath + ".meta";

        var metaContent = new StringReader(File.ReadAllText(textureMetaPath));
        var yaml = new YamlStream();
        yaml.Load(metaContent);

        var rootNode = yaml.Documents[0].RootNode;
        var guidNode = Utility.FindYamlChildNode((YamlMappingNode)rootNode, "guid");
        string guid = ((YamlScalarNode) guidNode).Value;
        var textureImporterNode = Utility.FindYamlChildNode((YamlMappingNode)rootNode, "TextureImporter");
        var assetBundleNameNode = Utility.FindYamlChildNode((YamlMappingNode)textureImporterNode, "assetBundleName");
        string assetBundleName = ((YamlScalarNode)assetBundleNameNode).Value;
        string cabFilename = Utility.GetCabFilename(assetBundleName);
        string cabRessFilename = Utility.GetCabRessFilename(cabFilename);
        string archivePath = Utility.GetArchivePath(cabFilename, cabRessFilename);

        // TODO: Replace jpeg loading library
        var unityTexture2D = AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(texturePath);
        byte[] textureRawData = unityTexture2D.GetRawTextureData();
        int width = unityTexture2D.width;
        int height = unityTexture2D.height;

        int textureFileId = 2800000;
        Int64 texturePathId = Utility.GetPathId(guid, textureFileId);

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
            blocksInfoAndDirectory.Write(endiannessWriterCompressed);

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
            serializedFileHeader.Write(endiannessWriterStorage);
            metadataSize = (Int32)endiannessWriterStorage.Position;

            endiannessWriterStorage.Align((int)serializedFileHeader.DataOffset);
            assetBundleObjectPosition = (Int32)endiannessWriterStorage.Position;
            assetBundleObjectOffset = (Int32)(assetBundleObjectPosition - serializedFileHeader.DataOffset);
            assetBundle.Write(endiannessWriterStorage, serializedFileHeader.Version);
            assetBundleObjectSize = (Int32)(endiannessWriterStorage.Position - assetBundleObjectPosition);

            // TODO: What is this padding?
            endiannessWriterStorage.WriteUInt32(0);

            texture2DPosition = (Int32)endiannessWriterStorage.Position;
            texture2DOffset = (Int32)(texture2DPosition - serializedFileHeader.DataOffset);
            texture2D.Write(endiannessWriterStorage);
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
            header.Write(endiannessWriter);
            endiannessWriter.WriteWithoutEndianness(compressedBuffer);
            endiannessWriter.WriteWithoutEndianness(serializeFileBuffer);
        }
    }
}