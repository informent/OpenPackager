using System.IO.Compression;
using System.Text.Json;
using OpenPackager;

var root = Path.Combine(Path.GetTempPath(), "openpackager-engine-test-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(Path.Combine(root, "app"));
await File.WriteAllTextAsync(Path.Combine(root, "app", "hello.txt"), "OpenPackager engine test");
await PackagingEngine.WriteChecksumsAsync(root);
await PackagingEngine.WriteManifestAsync(root, new { test = true, createdUtc = DateTime.UtcNow });
var zip = PackagingEngine.CreateZip(root);
if (!File.Exists(Path.Combine(root, "SHA256SUMS.txt"))) throw new Exception("Checksum file was not created.");
if (!File.Exists(Path.Combine(root, "openpackager-manifest.json"))) throw new Exception("Manifest was not created.");
if (!File.Exists(zip)) throw new Exception("ZIP was not created.");
using (var archive = ZipFile.OpenRead(zip))
{
    if (archive.GetEntry("openpackager-manifest.json") is null) throw new Exception("Manifest missing from ZIP.");
    if (archive.GetEntry("SHA256SUMS.txt") is null) throw new Exception("Checksums missing from ZIP.");
}
Directory.Delete(root, true);
Console.WriteLine("PASS: packaging engine created checksums, manifest, and ZIP contents");
