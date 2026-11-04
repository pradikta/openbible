using System.Runtime.CompilerServices;
using OpenBible.Domain.Catalog;
using OpenBible.Domain.Entities;
using OpenBible.Domain.Enums;
using OpenBible.Domain.Interfaces;
using OpenBible.Domain.ValueObjects;

namespace OpenBible.Infrastructure.Persistence;

public sealed class SampleScriptureSeeder
{
    public const string VersionId = "sample-kjv";

    private readonly IBibleRepository _bibleRepository;
    private readonly ISearchIndex _searchIndex;

    public SampleScriptureSeeder(IBibleRepository bibleRepository, ISearchIndex searchIndex)
    {
        _bibleRepository = bibleRepository;
        _searchIndex = searchIndex;
    }

    public async Task EnsureSeededAsync(CancellationToken ct = default)
    {
        var existing = await _bibleRepository.GetVersionAsync(VersionId, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            return;
        }

        var books = BibleBookCatalog.All
            .Select(book => new Book(book.Id, book.ShortName, book.FullName, book.ChapterCount))
            .ToList();

        var version = new BibleVersion(
            VersionId,
            "Sample (KJV excerpts)",
            "en",
            "Public-domain King James excerpts so the reader works offline before a .yes file is imported.",
            books);

        var verses = CreateVerses();
        var pericopes = CreatePericopes();

        await _bibleRepository
            .SaveVersionAsync(version, Enumerate(verses, ct), Enumerate(pericopes, ct), ct)
            .ConfigureAwait(false);

        await _searchIndex.IndexVersionAsync(VersionId, Enumerate(verses, ct), ct).ConfigureAwait(false);
    }

    private static List<Verse> CreateVerses()
    {
        return
        [
            Verse(BookId.Genesis, 1, 1, "In the beginning God created the heaven and the earth."),
            Verse(BookId.Genesis, 1, 2, "And the earth was without form, and void; and darkness was upon the face of the deep. And the Spirit of God moved upon the face of the waters."),
            Verse(BookId.Genesis, 1, 3, "And God said, Let there be light: and there was light."),
            Verse(BookId.Genesis, 1, 4, "And God saw the light, that it was good: and God divided the light from the darkness."),
            Verse(BookId.Genesis, 1, 5, "And God called the light Day, and the darkness he called Night. And the evening and the morning were the first day."),
            Verse(BookId.Psalms, 23, 1, "The LORD is my shepherd; I shall not want."),
            Verse(BookId.Psalms, 23, 2, "He maketh me to lie down in green pastures: he leadeth me beside the still waters."),
            Verse(BookId.Psalms, 23, 3, "He restoreth my soul: he leadeth me in the paths of righteousness for his name's sake."),
            Verse(BookId.Psalms, 23, 4, "Yea, though I walk through the valley of the shadow of death, I will fear no evil: for thou art with me; thy rod and thy staff they comfort me."),
            Verse(BookId.Psalms, 23, 5, "Thou preparest a table before me in the presence of mine enemies: thou anointest my head with oil; my cup runneth over."),
            Verse(BookId.Psalms, 23, 6, "Surely goodness and mercy shall follow me all the days of my life: and I will dwell in the house of the LORD for ever."),
            Verse(BookId.Matthew, 5, 3, "Blessed are the poor in spirit: for theirs is the kingdom of heaven."),
            Verse(BookId.Matthew, 5, 4, "Blessed are they that mourn: for they shall be comforted."),
            Verse(BookId.Matthew, 5, 5, "Blessed are the meek: for they shall inherit the earth."),
            Verse(BookId.Matthew, 5, 6, "Blessed are they which do hunger and thirst after righteousness: for they shall be filled."),
            Verse(BookId.Matthew, 5, 7, "Blessed are the merciful: for they shall obtain mercy."),
            Verse(BookId.Matthew, 5, 8, "Blessed are the pure in heart: for they shall see God."),
            Verse(BookId.Matthew, 5, 9, "Blessed are the peacemakers: for they shall be called the children of God."),
            Verse(BookId.John, 1, 1, "In the beginning was the Word, and the Word was with God, and the Word was God."),
            Verse(BookId.John, 1, 2, "The same was in the beginning with God."),
            Verse(BookId.John, 1, 3, "All things were made by him; and without him was not any thing made that was made."),
            Verse(BookId.John, 1, 4, "In him was life; and the life was the light of men."),
            Verse(BookId.John, 1, 5, "And the light shineth in darkness; and the darkness comprehended it not."),
            Verse(BookId.John, 3, 16, "For God so loved the world, that he gave his only begotten Son, that whosoever believeth in him should not perish, but have everlasting life.")
        ];
    }

    private static List<Pericope> CreatePericopes()
    {
        return
        [
            new Pericope(new VerseReference(BookId.Genesis, 1, 1), "The Creation", ["John 1:1-3"]),
            new Pericope(new VerseReference(BookId.Psalms, 23, 1), "The Lord Is My Shepherd"),
            new Pericope(new VerseReference(BookId.Matthew, 5, 3), "The Beatitudes"),
            new Pericope(new VerseReference(BookId.John, 1, 1), "The Word Became Flesh", ["Genesis 1:1"]),
            new Pericope(new VerseReference(BookId.John, 3, 16), "God So Loved the World")
        ];
    }

    private static Verse Verse(BookId book, int chapter, int verse, string text)
    {
        return new Verse(new VerseReference(book, chapter, verse), text);
    }

    private static async IAsyncEnumerable<T> Enumerate<T>(
        IReadOnlyList<T> items,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();
            yield return item;
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
