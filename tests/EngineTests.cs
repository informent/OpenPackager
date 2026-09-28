using System.IO.Compression;
using System.Text.Json;
using System.Security.Cryptography;
using OpenPackager;

var root = Path.Combine(Path.GetTempPath(), "openpackager-engine-test-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(Path.Combine(root, "app"));
await File.WriteAllTextAsync(Path.Combine(root, "app", "hello.txt"), "OpenPackager engine test");
await File.WriteAllTextAsync(Path.Combine(root, "app", "résumé with spaces.txt"), "Unicode content");
var large = Path.Combine(root, "app", "large.bin");
using (var stream = File.Create(large)) stream.SetLength(32 * 1024 * 1024);
var zip = await PackagingEngine.CompleteReleaseAsync(root, new { test = true, createdUtc = DateTime.UtcNow });
if (!File.Exists(Path.Combine(root, "SHA256SUMS.txt"))) throw new Exception("Checksum file was not created.");
if (!File.Exists(Path.Combine(root, "openpackager-manifest.json"))) throw new Exception("Manifest was not created.");
if (!File.Exists(zip)) throw new Exception("ZIP was not created.");
using (var archive = ZipFile.OpenRead(zip))
{
    if (archive.GetEntry("openpackager-manifest.json") is null) throw new Exception("Manifest missing from ZIP.");
    if (archive.GetEntry("SHA256SUMS.txt") is null) throw new Exception("Checksums missing from ZIP.");
    using var reader = new StreamReader(archive.GetEntry("SHA256SUMS.txt")!.Open());
    var lines = (await reader.ReadToEndAsync()).Split('\n', StringSplitOptions.RemoveEmptyEntries);
    if (lines.Length != archive.Entries.Count - 1) throw new Exception("Checksum coverage is incomplete.");
    foreach (var raw in lines)
    {
        var line = raw.TrimEnd('\r');
        var name = line[66..];
        if (name == "SHA256SUMS.txt") throw new Exception("Checksum file includes itself.");
        using var content = archive.GetEntry(name)?.Open() ?? throw new Exception("Missing archived file: " + name);
        if (!Convert.ToHexString(SHA256.HashData(content)).Equals(line[..64], StringComparison.OrdinalIgnoreCase))
            throw new Exception("Archived checksum mismatch: " + name);
    }
}
var before = await File.ReadAllTextAsync(Path.Combine(root, "SHA256SUMS.txt"));
await PackagingEngine.WriteChecksumsAsync(root);
if (before != await File.ReadAllTextAsync(Path.Combine(root, "SHA256SUMS.txt"))) throw new Exception("Repeated checksums changed.");
var zipHash = SHA256.HashData(await File.ReadAllBytesAsync(zip));
try { PackagingEngine.CreateZip(root + Path.DirectorySeparatorChar); throw new Exception("Existing ZIP was overwritten."); }
catch (IOException) { }
var preservedHash = SHA256.HashData(await File.ReadAllBytesAsync(zip));
if (!zipHash.SequenceEqual(preservedHash)) throw new Exception("Existing ZIP changed.");
File.Delete(zip);
var lockedFile = Path.Combine(root, "app", "hello.txt");
using (var locked = new FileStream(lockedFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
{
    try { PackagingEngine.CreateZip(root); throw new Exception("Locked file should fail packaging."); }
    catch (IOException) { }
}
if (File.Exists(zip) || Directory.EnumerateFiles(Path.GetDirectoryName(root)!, Path.GetFileName(root) + ".zip.*.tmp").Any())
    throw new Exception("Failed ZIP left output behind.");
Directory.Delete(root, true);
Console.WriteLine("PASS: archive hashes, manifest coverage, Unicode, large file, repeated checksums, existing ZIP protection and failure cleanup");
