using System.IO.Compression;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace OpenPackager;

public static class PackagingEngine
{
    public static async Task<string> WriteChecksumsAsync(string releaseFolder)
    {
        var root = Path.GetFullPath(releaseFolder);
        var path = Path.Combine(root, "SHA256SUMS.txt");
        var files = EnumerateRegularFiles(root).Where(file => !string.Equals(file, path, StringComparison.OrdinalIgnoreCase))
            .OrderBy(file => Path.GetRelativePath(root, file), StringComparer.Ordinal).ToArray();
        var lines = new List<string>();
        foreach (var file in files)
        {
            await using var stream = File.OpenRead(file);
            var hash = await SHA256.HashDataAsync(stream);
            lines.Add($"{Convert.ToHexString(hash).ToLowerInvariant()}  {Path.GetRelativePath(root, file).Replace('\\', '/')}");
        }
        await File.WriteAllLinesAsync(path, lines);
        return path;
    }

    public static async Task<string> WriteManifestAsync(string releaseFolder, object manifest)
    {
        var path = Path.Combine(releaseFolder, "openpackager-manifest.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }

    // Refuse linked content instead of silently including files outside the release.
    static IEnumerable<string> EnumerateRegularFiles(string folder)
    {
        if ((File.GetAttributes(folder) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Release folders must not contain symbolic links or junctions.");
        foreach (var entry in Directory.EnumerateFileSystemEntries(folder))
        {
            var attributes = File.GetAttributes(entry);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Release folders must not contain symbolic links or junctions.");
            if ((attributes & FileAttributes.Directory) != 0)
            {
                foreach (var file in EnumerateRegularFiles(entry)) yield return file;
            }
            else yield return entry;
        }
    }

    public static async Task<string> CompleteReleaseAsync(string releaseFolder, object manifest)
    {
        await WriteManifestAsync(releaseFolder, manifest);
        await WriteChecksumsAsync(releaseFolder);
        return CreateZip(releaseFolder);
    }

    public static string CreateZip(string releaseFolder)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(releaseFolder));
        var zip = root + ".zip";
        if (File.Exists(zip)) throw new IOException("The release ZIP already exists. Choose a new output folder.");
        var temporary = zip + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var files = EnumerateRegularFiles(root).OrderBy(file => file, StringComparer.Ordinal).ToArray();
            using (var archive = ZipFile.Open(temporary, ZipArchiveMode.Create))
                foreach (var file in files)
                    archive.CreateEntryFromFile(file, Path.GetRelativePath(root, file).Replace('\\', '/'), CompressionLevel.Optimal);
            File.Move(temporary, zip);
            return zip;
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
