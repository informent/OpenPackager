using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace OpenPackager;

public static class PackagingEngine
{
    public static async Task<string> WriteChecksumsAsync(string releaseFolder)
    {
        var files = Directory.EnumerateFiles(releaseFolder, "*", SearchOption.AllDirectories).OrderBy(x => x).ToArray();
        var lines = files.Select(file => $"{Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))).ToLowerInvariant()}  {Path.GetRelativePath(releaseFolder, file).Replace('\\', '/')}");
        var path = Path.Combine(releaseFolder, "SHA256SUMS.txt");
        await File.WriteAllLinesAsync(path, lines);
        return path;
    }

    public static async Task<string> WriteManifestAsync(string releaseFolder, object manifest)
    {
        var path = Path.Combine(releaseFolder, "openpackager-manifest.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }

    public static string CreateZip(string releaseFolder)
    {
        var zip = releaseFolder.TrimEnd(Path.DirectorySeparatorChar) + ".zip";
        if (File.Exists(zip)) File.Delete(zip);
        ZipFile.CreateFromDirectory(releaseFolder, zip, CompressionLevel.Optimal, false);
        return zip;
    }
}
