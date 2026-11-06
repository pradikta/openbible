using OpenBible.Domain.Enums;

namespace OpenBible.Domain.Interfaces;

public interface IBibleFormatReaderSelector
{
    IBibleFormatReader GetReader(BibleFormat format);

    /// <summary>Detects the format from the stream signature; returns null when unknown.</summary>
    BibleFormat? DetectFormat(Stream? stream);
}
