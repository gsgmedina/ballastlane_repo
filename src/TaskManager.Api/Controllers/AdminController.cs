using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Api.Controllers;

/// <summary>
/// Operational helpers. The DB export streams a consistent snapshot of the live
/// SQLite file so it can be opened locally (e.g. in DB Browser for SQLite).
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize]
public sealed class AdminController : ControllerBase
{
    private readonly IServiceProvider _services;

    public AdminController(IServiceProvider services) => _services = services;

    /// <summary>Downloads a snapshot copy of the live SQLite database. (Authorized)</summary>
    [HttpGet("db")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DownloadDatabase(CancellationToken ct)
    {
        // Only available when running on the SQLite provider.
        var factory = _services.GetService(typeof(ISqliteConnectionFactory)) as ISqliteConnectionFactory;
        if (factory is null)
            return BadRequest(new { message = "Database export is only available for the SQLite provider." });

        // VACUUM INTO produces a clean, consistent snapshot even while the DB is in use.
        var snapshotPath = Path.Combine(Path.GetTempPath(), $"tm-export-{Guid.NewGuid():N}.db");
        await using (var conn = factory.Create())
        {
            await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "VACUUM INTO $path";
            cmd.Parameters.AddWithValue("$path", snapshotPath);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        try
        {
            var bytes = await System.IO.File.ReadAllBytesAsync(snapshotPath, ct);
            return File(bytes, "application/octet-stream", "taskmanager-live.db");
        }
        finally
        {
            if (System.IO.File.Exists(snapshotPath))
                System.IO.File.Delete(snapshotPath);
        }
    }
}
