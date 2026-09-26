using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

const string Version = "0.1.0";
if (args.Length == 0 || args[0] is "--help" or "-h") { Help(); return; }
var command = args[0].ToLowerInvariant();
if (args.Length < 2) { Console.Error.WriteLine("A project directory is required."); Environment.ExitCode = 2; return; }
var project = Path.GetFullPath(args[1]);
if (!Directory.Exists(project)) { Console.Error.WriteLine($"Project directory not found: {project}"); Environment.ExitCode = 2; return; }
var info = Detect(project);
switch (command)
{
    case "detect": Print(info); break;
    case "validate":
        var issues = Validate(info);
        foreach (var issue in issues) Console.WriteLine($"{(issue.IsError ? "ERROR" : "WARN ")}: {issue.Message}");
        Console.WriteLine(issues.Count == 0 ? "Validation passed." : $"Validation completed with {issues.Count} finding(s).");
        Environment.ExitCode = issues.Any(i => i.IsError) ? 1 : 0; break;
    case "package": await Package(project, info, args[2..]); break;
    default: Console.Error.WriteLine($"Unknown command: {command}"); Environment.ExitCode = 2; break;
}

static ProjectInfo Detect(string root)
{
    var csproj = Directory.EnumerateFiles(root, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
    var py = File.Exists(Path.Combine(root, "pyproject.toml")) || File.Exists(Path.Combine(root, "requirements.txt"));
    var node = File.Exists(Path.Combine(root, "package.json"));
    var kind = csproj is not null ? "dotnet" : py ? "python" : node ? "node" : "unknown";
    return new(root, kind, csproj, py, node);
}
static List<Finding> Validate(ProjectInfo p)
{
    var result = new List<Finding>();
    if (p.Kind == "unknown") result.Add(new(true, "No supported project marker was found."));
    if (p.Kind == "dotnet" && !File.Exists(Path.Combine(p.Root, "README.md"))) result.Add(new(false, "README.md is missing; consider including usage instructions."));
    if (Directory.Exists(Path.Combine(p.Root, "bin")) || Directory.Exists(Path.Combine(p.Root, "obj"))) result.Add(new(false, "Build output folders are present; they will not be copied into the release."));
    return result;
}
static async Task Package(string root, ProjectInfo info, string[] options)
{
    if (info.Kind != "dotnet") { Console.Error.WriteLine("The first release packages .NET projects. Python and Node adapters are planned next."); Environment.ExitCode = 1; return; }
    var output = Option(options, "--output") ?? Path.Combine(root, "dist");
    var runtime = Option(options, "--runtime") ?? "win-x64";
    var self = options.Contains("--self-contained");
    var single = options.Contains("--single-file");
    var release = Path.Combine(Path.GetFullPath(output), Path.GetFileNameWithoutExtension(info.Csproj!));
    Directory.CreateDirectory(release);
    var publishDir = Path.Combine(release, "app");
    var dotnetArgs = $"publish \"{info.Csproj}\" -c Release -r {runtime} --self-contained {self.ToString().ToLowerInvariant()} -o \"{publishDir}\"" + (single ? " -p:PublishSingleFile=true" : "");
    Console.WriteLine($"Publishing {info.Csproj} for {runtime}...");
    var exit = await Run("dotnet", dotnetArgs, root);
    if (exit != 0) { Environment.ExitCode = exit; return; }
    var files = Directory.EnumerateFiles(publishDir, "*", SearchOption.AllDirectories).OrderBy(x => x).ToArray();
    var checksums = files.Select(f => $"{Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(f))).ToLowerInvariant()}  {Path.GetRelativePath(release, f).Replace('\\', '/')}");
    await File.WriteAllLinesAsync(Path.Combine(release, "SHA256SUMS.txt"), checksums);
    var manifest = new { tool = "OpenPackager", version = Version, createdUtc = DateTime.UtcNow, project = Path.GetFileNameWithoutExtension(info.Csproj!), runtime, selfContained = self, singleFile = single, fileCount = files.Length };
    await File.WriteAllTextAsync(Path.Combine(release, "openpackager-manifest.json"), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"Package ready: {release}");
}
static async Task<int> Run(string file, string arguments, string workingDirectory)
{
    using var p = Process.Start(new ProcessStartInfo(file, arguments) { WorkingDirectory = workingDirectory, UseShellExecute = false });
    if (p is null) return 1; await p.WaitForExitAsync(); return p.ExitCode;
}
static string? Option(string[] args, string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
static void Print(ProjectInfo p) => Console.WriteLine(JsonSerializer.Serialize(p, new JsonSerializerOptions { WriteIndented = true }));
static void Help() => Console.WriteLine("OpenPackager 0.1.0\n\nCommands:\n  detect <project>\n  validate <project>\n  package <project> [--output DIR] [--runtime RID] [--self-contained] [--single-file]");
record ProjectInfo(string Root, string Kind, string? Csproj, bool HasPythonMarker, bool HasNodeMarker);
record Finding(bool IsError, string Message);
