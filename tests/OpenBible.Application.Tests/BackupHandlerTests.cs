using NSubstitute;
using OpenBible.Application.Backup.Commands.ImportBackup;
using OpenBible.Application.Backup.Queries.ExportBackup;
using OpenBible.Domain.Interfaces;
using Xunit;

namespace OpenBible.Application.Tests;

public sealed class BackupHandlerTests
{
    private readonly IBackupService _backup = Substitute.For<IBackupService>();

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Fact]
    public async Task Export_ReturnsTimestampedFile()
    {
        _backup.ExportAsync(Arg.Any<CancellationToken>()).Returns("{}");
        var clock = new FixedTime(new DateTimeOffset(2026, 11, 4, 12, 30, 15, TimeSpan.Zero));

        var result = await new ExportBackupHandler(_backup, clock).Handle(new ExportBackupQuery(), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("openbible-backup-20261104-123015.json", result.Value!.FileName);
        Assert.Equal("{}", result.Value.Content);
    }

    [Fact]
    public async Task Import_Valid_Succeeds()
    {
        var result = await new ImportBackupHandler(_backup).Handle(new ImportBackupCommand("{}"), default);

        Assert.True(result.IsSuccess);
        await _backup.Received(1).ImportAsync("{}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Import_InvalidBackup_ReturnsFailure()
    {
        _backup.ImportAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidDataException("bad file")));

        var result = await new ImportBackupHandler(_backup).Handle(new ImportBackupCommand("x"), default);

        Assert.True(result.IsFailure);
        Assert.Equal("bad file", result.Error);
    }

    [Fact]
    public void Validator_RejectsEmptyJson() =>
        Assert.False(new ImportBackupValidator().Validate(new ImportBackupCommand("")).IsValid);
}
