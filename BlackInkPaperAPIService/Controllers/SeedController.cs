using Asp.Versioning;
using Infrastructure.Persistence.Seeding;
using Microsoft.AspNetCore.Mvc;

namespace BlackInkPaperAPIService.Controllers;

/// <summary>
/// Development-only seeding. Delegates to <see cref="ISeedRunner"/>, the same component the
/// `dotnet run -- --seed` command line uses, so HTTP and CLI can never drift apart.
/// Outside Development every action returns 404, as though the controller did not exist.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/seed")]
public class SeedController(ISeedRunner seedRunner, IWebHostEnvironment env) : ControllerBase
{
    /// <summary>Lists the seed sets this build knows how to run.</summary>
    [HttpGet]
    public IActionResult ListSets()
        => env.IsDevelopment() ? Ok(new { Sets = seedRunner.AvailableSets }) : NotFound();

    /// <summary>
    /// Runs a seed set: identity, catalog, artwork or all. Every set starts by applying EF
    /// Core migrations, so it works against a completely empty database.
    /// </summary>
    [HttpPost("{set}")]
    public async Task<IActionResult> Run(string set, CancellationToken ct)
    {
        if (!env.IsDevelopment()) return NotFound();

        var report = await seedRunner.RunAsync(set, ct);

        if (report.Success)
            return Ok(new { report.Set, StepsRun = report.StepsRun, Message = "Seeding completed." });

        // An unknown set name is a bad request; a step that blew up is a server-side failure.
        return seedRunner.AvailableSets.Contains(set, StringComparer.OrdinalIgnoreCase)
            ? StatusCode(500, new { report.Set, report.StepsRun, report.Error })
            : BadRequest(new { report.Set, report.Error });
    }
}
