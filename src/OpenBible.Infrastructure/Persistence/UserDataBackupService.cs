using System.Text.Json;
using OpenBible.Domain.Interfaces;
using OpenBible.Infrastructure.Persistence.Serialization;

namespace OpenBible.Infrastructure.Persistence;

/// <summary>
/// Backs up user data as one versioned JSON document. Installed Bible texts are not included;
/// they are re-installed from the catalog or a .yes file.
/// </summary>
public sealed class UserDataBackupService(
    PersistentUserDataRepository userData,
    PersistentStudyDataRepository study,
    PersistentAppSettingsRepository settings) : IBackupService
{
    public const string FormatName = "openbible-backup";
    public const int CurrentVersion = 1;

    public async Task<string> ExportAsync(CancellationToken ct = default)
    {
        var backup = new StoredBackup(
            FormatName,
            CurrentVersion,
            DateTimeOffset.UtcNow,
            userData.Export(),
            study.Export(),
            new StoredAppSettings(await settings.GetDefaultVersionIdAsync(ct).ConfigureAwait(false)));
        return JsonSerializer.Serialize(backup, BackupJsonContext.Default.StoredBackup);
    }

    public async Task ImportAsync(string json, CancellationToken ct = default)
    {
        var backup = Parse(json);
        var previousUserData = userData.Export();
        var previousStudy = study.Export();
        var previousDefault = await settings.GetDefaultVersionIdAsync(ct).ConfigureAwait(false);

        try
        {
            if (backup.UserData is not null)
            {
                await userData.ReplaceAsync(backup.UserData, ct).ConfigureAwait(false);
            }

            if (backup.Study is not null)
            {
                await study.ReplaceAsync(backup.Study, ct).ConfigureAwait(false);
            }

            if (backup.Settings is not null)
            {
                await settings.SetDefaultVersionIdAsync(backup.Settings.DefaultVersionId, ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or NullReferenceException)
        {
            await userData.ReplaceAsync(previousUserData, ct).ConfigureAwait(false);
            await study.ReplaceAsync(previousStudy, ct).ConfigureAwait(false);
            await settings.SetDefaultVersionIdAsync(previousDefault, ct).ConfigureAwait(false);
            throw new InvalidDataException("The backup contains invalid data. Nothing was changed.", ex);
        }
    }

    private static StoredBackup Parse(string json)
    {
        StoredBackup? backup;
        try
        {
            backup = JsonSerializer.Deserialize(json, BackupJsonContext.Default.StoredBackup);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("This file is not a valid OpenBible backup.", ex);
        }

        if (backup is null || backup.Format != FormatName)
        {
            throw new InvalidDataException("This file is not a valid OpenBible backup.");
        }

        if (backup.Version > CurrentVersion)
        {
            throw new InvalidDataException("This backup was made by a newer version of OpenBible.");
        }

        return backup;
    }
}
