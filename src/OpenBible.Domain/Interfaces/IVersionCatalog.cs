using OpenBible.Domain.Entities;

namespace OpenBible.Domain.Interfaces;

/// <summary>Offline list of translations that can be downloaded and then imported.</summary>
public interface IVersionCatalog
{
    IReadOnlyList<CatalogVersion> GetAll();
}
