using System;

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

    public Int32 StreamingMipmapsPriority;
    public Int32 ImageCount;
    public Int32 TextureDimension;
    public GLTextureSettings TextureSettings;
    public Int32 LightmapFormat;
    public Int32 ColorSpace;
    public Int32 ImageDataSize;
    public StreamingInfo StreamData;
}