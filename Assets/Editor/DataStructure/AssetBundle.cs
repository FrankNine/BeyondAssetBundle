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
}