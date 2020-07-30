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

    internal void Write(EndiannessWriter writer)
    {
        writer.WriteAlignedString(Name);

        writer.WriteInt32(ForcedFallbackFormat);
        writer.WriteBoolean(DownscaleFallback);
        writer.Align(4);

        writer.WriteInt32(Width);
        writer.WriteInt32(Height);
        writer.WriteInt32(CompleteImageSize);
        writer.WriteInt32((Int32) TextureFormat);
        writer.WriteInt32(MipCount);

        writer.WriteBoolean(IsReadable);
        writer.WriteBoolean(IsReadAllowed);
        writer.Align(4);

        writer.WriteInt32(StreamingMipmapsPriority);
        writer.WriteInt32(ImageCount);
        writer.WriteInt32(TextureDimension);

        writer.WriteInt32(TextureSettings.FilterMode);
        writer.WriteInt32(TextureSettings.Aniso);
        writer.WriteSingle(TextureSettings.MipBias);
        writer.WriteInt32(TextureSettings.WrapMode);
        writer.WriteInt32(TextureSettings.WrapV);
        writer.WriteInt32(TextureSettings.WrapW);

        writer.WriteInt32(LightmapFormat);
        writer.WriteInt32(ColorSpace);
        writer.WriteInt32(ImageDataSize);

        writer.WriteUInt32(StreamData.Offset);
        writer.WriteUInt32(StreamData.Size);
        writer.WriteAlignedString(StreamData.Path);
    }
}