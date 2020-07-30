public class BlocksInfoAndDirectory
{
    public byte[] UncompressedDataHash = new byte[16] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
    public StorageBlock[] StorageBlocks;
    public Node[] Nodes;

    internal void Write(EndiannessWriter writer)
    {
        writer.Write(UncompressedDataHash);

        writer.WriteInt32(StorageBlocks.Length);
        foreach (var storageBlock in StorageBlocks)
        {
            writer.WriteUInt32(storageBlock.CompressedSize);
            writer.WriteUInt32(storageBlock.UncompressedSize);
            writer.WriteUInt16(storageBlock.Flags);
        }

        writer.WriteInt32(Nodes.Length);
        foreach (var node in Nodes)
        {
            writer.WriteInt64(node.Offset);
            writer.WriteInt64(node.Size);
            writer.WriteUInt32(node.Flags);
            writer.WriteString(node.Path);
        }
    }
}