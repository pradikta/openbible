namespace OpenBible.Infrastructure.Sword;

internal sealed class SwordTestamentFiles(byte[] bzv, byte[] bzs, byte[] bzz)
{
    public byte[] Bzv { get; } = bzv;
    public byte[] Bzs { get; } = bzs;
    public byte[] Bzz { get; } = bzz;
    public uint? LastBufferNumber { get; set; }
    public byte[]? LastBufferBytes { get; set; }
}
