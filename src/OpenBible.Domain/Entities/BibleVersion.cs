namespace OpenBible.Domain.Entities;

public sealed class BibleVersion
{
    public string Id { get; }
    public string Title { get; }
    public string Language { get; }
    public string Description { get; }
    public IReadOnlyList<Book> Books { get; }

    public BibleVersion(string id, string title, string language, string description, IReadOnlyList<Book>? books = null)
    {
        Id = !string.IsNullOrWhiteSpace(id) ? id : throw new ArgumentException("Version ID cannot be empty.", nameof(id));
        Title = !string.IsNullOrWhiteSpace(title) ? title : throw new ArgumentException("Version Title cannot be empty.", nameof(title));
        Language = language ?? "en";
        Description = description ?? string.Empty;
        Books = books ?? Array.Empty<Book>();
    }
}
