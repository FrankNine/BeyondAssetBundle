using System;
using System.Collections.Generic;

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

    internal void Write(EndiannessWriter writer, UInt32 serializationVersion)
    {
        writer.WriteAlignedString(Name);

        writer.WritePPtrArray(PreloadTable, serializationVersion);
        writer.WriteInt32(Container.Length);
        foreach (var container in Container)
        {
            writer.WriteAlignedString(container.Key);
            writer.WriteAssetInfo(container.Value, serializationVersion);
        }

        writer.WriteAssetInfo(MainAsset, serializationVersion);

        writer.WriteUInt32(RuntimeCompatibility);

        writer.WriteAlignedString(AssetBundleName);
        writer.WriteInt32(DependencyAssetBundleNames.Length);
        foreach (var dependencyAssetBundleName in DependencyAssetBundleNames)
        {
            writer.WriteAlignedString(dependencyAssetBundleName);
        }

        writer.WriteBoolean(IsStreamedSceneAssetBundle);
        writer.Align(4);
        writer.WriteInt32(ExplicitDataLayout);
        writer.WriteInt32(PathFlags);

        writer.WriteInt32(SceneHashes.Count);
        foreach (var sceneHash in SceneHashes)
        {
            writer.WriteString(sceneHash.Key);
            writer.WriteString(sceneHash.Value);
        }
    }
}