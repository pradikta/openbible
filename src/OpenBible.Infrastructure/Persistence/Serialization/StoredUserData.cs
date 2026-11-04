namespace OpenBible.Infrastructure.Persistence.Serialization;

internal sealed record StoredUserData(
    StoredPosition[] Positions,
    StoredBookmark[] Bookmarks,
    StoredHighlight[] Highlights);
