using MediatR;
using OpenBible.Application.Common.Models;

namespace OpenBible.Application.Backup.Commands.ImportBackup;

public sealed record ImportBackupCommand(string Json) : IRequest<Result>;
