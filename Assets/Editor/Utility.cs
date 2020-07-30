using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YamlDotNet.RepresentationModel;

internal static class Utility
{
    internal static YamlNode FindYamlChildNode(YamlMappingNode node, string tag)
    {
        foreach (var entry in node.Children)
        {
            if (((YamlScalarNode) entry.Key).Value == tag)
            {
                return entry.Value;
            }
        }

        return null;
    }

    internal static Int64 GetPathId(string guid, Int64 fileId)
    {
        var input = new List<byte>();
        input.AddRange(Encoding.ASCII.GetBytes(guid));
        input.AddRange(BitConverter.GetBytes((Int32) 3));
        input.AddRange(BitConverter.GetBytes(fileId));

        var output = Md4.Md4Hash(input);
        return BitConverter.ToInt64(output.Take(8).ToArray(), 0);
    }

    internal static string GetCabFilename(string assetBundleName)
        => "CAB-" + StringFromByteArray(Md4.Md4Hash(new List<byte>(Encoding.ASCII.GetBytes(assetBundleName)))).ToLower();

    internal static string StringFromByteArray(byte[] byteArray)
        => BitConverter.ToString(byteArray).Replace("-", "");

    internal static string GetCabRessFilename(string cabFilename)
        => cabFilename + ".resS";

    internal static string GetArchivePath(string cabFilename, string cabRessFilename)
        => $"archive:/{cabFilename}/{cabRessFilename}";
}