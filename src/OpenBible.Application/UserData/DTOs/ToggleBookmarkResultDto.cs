namespace OpenBible.Application.UserData.DTOs;

public sealed record ToggleBookmarkResultDto(bool IsBookmarked, BookmarkDto? Bookmark);
