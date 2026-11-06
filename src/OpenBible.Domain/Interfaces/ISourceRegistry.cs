using OpenBible.Domain.Entities;

namespace OpenBible.Domain.Interfaces;

public interface ISourceRegistry
{
    IReadOnlyList<IBibleSource> Sources { get; }
    IBibleSource? Find(string sourceId);
}
