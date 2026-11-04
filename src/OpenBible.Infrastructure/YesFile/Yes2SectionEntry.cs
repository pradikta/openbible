namespace OpenBible.Infrastructure.YesFile;

public sealed record Yes2SectionEntry(string Name, long StartOffset, int AttributesSize, int ContentSize)
{
    public long ContentOffset => checked(StartOffset + AttributesSize);
}
