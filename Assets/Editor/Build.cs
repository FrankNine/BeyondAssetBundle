using System;
using System.IO;
using System.Text;
using UnityEditor;

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

    private enum Endianness
    {
        Little = 0,
        Big = 1
    }

    [MenuItem("AssetBundle/Build Counterfeit")]
    public static void BuildCounterfeit()
    {
        using (var fileStream = File.Create("Counterfeit/texture"))
        using (var binaryWriter = new BinaryWriter(fileStream))
        {
            var fileEndianness = Endianness.Little;

            string signature = "UnityFS";
            WriteString(binaryWriter, fileEndianness, signature);

            Int32 versionHead = 6;
            WriteUInt32(binaryWriter, fileEndianness, versionHead);

            string unityVersionHead = "5.x.x";
            WriteString(binaryWriter, fileEndianness, unityVersionHead);

            string unityRevision = "2018.4.14f1";
            WriteString(binaryWriter, fileEndianness, unityRevision);

            Int64 size = 8142;
            WriteUInt64(binaryWriter, fileEndianness, size);


            Int32 compressedBlocksInfoSize = 84;
            WriteUInt32(binaryWriter, fileEndianness, compressedBlocksInfoSize);

            Int32 uncompressedBlocksInfoSize = 153;
            WriteUInt32(binaryWriter, fileEndianness, uncompressedBlocksInfoSize);

            Int32 flags = 67;
            WriteUInt32(binaryWriter, fileEndianness, flags);

            int metadataSize = 165;
            WriteUInt32(binaryWriter, fileEndianness, metadataSize);

            int fileSize = 4936;
            WriteUInt32(binaryWriter, fileEndianness, fileSize);

            int version = 17;
            WriteUInt32(binaryWriter, fileEndianness, version);

            int dataOffset = 4096;
            WriteUInt32(binaryWriter, fileEndianness, dataOffset);

            byte endianness = (byte)fileEndianness;
            binaryWriter.Write(endianness);

            byte[] reserved = { 0, 0, 0 };
            binaryWriter.Write(reserved);

            string unityVersion = "2018.4.14f1\n2";
            WriteString(binaryWriter, fileEndianness, unityVersion);
        }
    }

    private static void WriteUInt32(BinaryWriter writer, Endianness endianness, Int32 value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (endianness == Endianness.Little)
        {
            Array.Reverse(bytes);
        }
        writer.Write(bytes);
    }

    private static void WriteUInt64(BinaryWriter writer, Endianness endianness, Int64 value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (endianness == Endianness.Little)
        {
            Array.Reverse(bytes);
        }
        writer.Write(bytes);
    }

    private static void WriteString(BinaryWriter writer, Endianness endianness, string value)
    {
        const byte zero = 0;
        writer.Write(Encoding.UTF8.GetBytes(value));
        writer.Write(zero);
    }
}
