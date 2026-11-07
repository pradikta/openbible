namespace OpenBible.Server.Sources;

public sealed record CatalogCacheInfo(int ItemCount, DateTimeOffset FetchedAt, bool Expired);
