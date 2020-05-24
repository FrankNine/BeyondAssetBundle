using System;
using System.ComponentModel;
using System.IO;
using K4os.Compression.LZ4;
using UnityEditor;


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
            BuildTarget.Android
        );
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

    private const int kArchiveCompressionTypeMask = 0x3F;

    [MenuItem("AssetBundle/Build Counterfeit")]
    public static void BuildCounterfeit()
    {
        var fileEndianness = Endianness.Little;

        using (var fileStream = File.Create("Counterfeit/texture"))
        using (var binaryWriter = new BinaryWriter(fileStream))
        using(var endiannessWriter = new EndiannessWriter(binaryWriter, Endianness.Little))
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
            using (var endiannessWriter2 = new EndiannessWriter(binaryWriter2, Endianness.Little))
            {
                byte[] uncompressedDataHash = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
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
            LZ4Codec.Encode(buffer, 0, uncompressedBlocksInfoSize, compressedBuffer, 0, compressedBlocksInfoSize, LZ4Level.L11_OPT);
           
            endiannessWriter.WriteWithoutEndianness(compressedBuffer);  


            UInt32 metadataSize = 165;
            endiannessWriter.WriteUInt32(metadataSize);

            UInt32 fileSize = 4936;
            endiannessWriter.WriteUInt32(fileSize);

            UInt32 version = 17;
            endiannessWriter.WriteUInt32(version);

            UInt32 dataOffset = 4096;
            endiannessWriter.WriteUInt32(dataOffset);

            byte endianness = (byte)fileEndianness;
            binaryWriter.Write(endianness);

            byte[] reserved = { 0, 0, 0 };
            binaryWriter.Write(reserved);

            string unityVersion = "2018.4.14f1\n2";
            endiannessWriter.WriteString(unityVersion);
        }
    }
}