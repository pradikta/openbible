using OpenBible.Domain.Entities;

namespace OpenBible.Server.Sources;

public sealed record CachedCatalog(DateTimeOffset FetchedAt, IReadOnlyList<SourceTranslation> Items);
