using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Swashbuckle.AspNetCore.Swagger;

namespace BlackInkPaperAPIService.Swagger;

/// <summary>
/// Writes the generated OpenAPI document to a file so it can be committed alongside the code.
///
/// The obvious tool for this, <c>Swashbuckle.AspNetCore.Cli</c>, cannot start this app — its host
/// resolution falls back to scanning for a <c>Startup</c> class, which minimal hosting does not
/// have. Generating from inside the already-built host sidesteps that, needs no extra tool, and
/// documents exactly the pipeline that ships rather than a re-hosted approximation.
/// </summary>
public static class OpenApiFileWriter
{
    private const string Argument = "--dump-openapi";
    private const string DefaultPath = "docs/api/swagger-v1.json";
    private const string SolutionFile = "BlackInkPaperAPIService.sln";
    private const string DocumentName = "v1";

    /// <summary>
    /// Handles <c>--dump-openapi[=path]</c>. Returns the process exit code when the argument was
    /// present, or <c>null</c> to let the caller carry on and start the server normally.
    /// </summary>
    public static async Task<int?> TryWriteAsync(WebApplication app, string[] args)
    {
        var arg = args.FirstOrDefault(a =>
            a == Argument || a.StartsWith(Argument + "=", StringComparison.Ordinal));

        if (arg is null) return null;

        var relativePath = arg.Contains('=') ? arg.Split('=', 2)[1] : DefaultPath;

        try
        {
            // Anchored to the solution directory, not the content root or the working directory:
            // `dotnet run` from the project folder and `dotnet run --project` from the repo root
            // (what CI does) produce different content roots, and a relative path would land the
            // file somewhere different in each.
            var path = Path.GetFullPath(Path.Combine(SolutionDirectory(app.Environment.ContentRootPath), relativePath));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var document = app.Services
                .GetRequiredService<ISwaggerProvider>()
                .GetSwagger(DocumentName);

            await File.WriteAllTextAsync(path, document.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0));

            Console.WriteLine($"OpenAPI document '{DocumentName}' written to {path}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to write OpenAPI document: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Walks up from <paramref name="startPath"/> to the directory holding the solution file,
    /// falling back to the start path when it is not found (a published app has no solution
    /// beside it, but it has no reason to export the spec either).
    /// </summary>
    private static string SolutionDirectory(string startPath)
    {
        for (var dir = new DirectoryInfo(startPath); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, SolutionFile)))
                return dir.FullName;
        }

        return startPath;
    }
}
