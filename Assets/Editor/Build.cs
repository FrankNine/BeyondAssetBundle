using System;
using System.ComponentModel;
using System.IO;
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
            string signature = "UnityFS";
            endiannessWriter.WriteString(signature);

            Int32 versionHead = 6;
            endiannessWriter.WriteInt32(versionHead);

            string unityVersionHead = "5.x.x";
            endiannessWriter.WriteString(unityVersionHead);

            string unityRevision = "2018.4.14f1";
            endiannessWriter.WriteString(unityRevision);

            Int64 size = 8142;
            endiannessWriter.WriteInt64(size);

            byte[] uncompressedDataHash = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            Int32 blocksInfoCount = 1;

            var storageBlock = new StorageBlock
            {
                CompressedSize = 8008,
                UncompressedSize = 8008,
                Flags = 64
            };

            Int32 nodeCount = 2;

            var cabNode = new Node
            {
                Offset = 0,
                Size = 4936,
                Flags = 4,
                Path = "CAB-f04fab77212e693fb63bdad7458f66fe"
            };

            var cabResSNode = new Node
            {
                Offset = 4936,
                Size = 3072,
                Flags = 0,
                Path = "CAB-f04fab77212e693fb63bdad7458f66fe.resS"
            };


            Int32 compressedBlocksInfoSize = 84;
            endiannessWriter.WriteInt32(compressedBlocksInfoSize);

            Int32 uncompressedBlocksInfoSize = 153;
            endiannessWriter.WriteInt32(uncompressedBlocksInfoSize);

            Int32 flags = 67;
            endiannessWriter.WriteInt32(flags);

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