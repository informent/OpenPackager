using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.IO.Compression;
using System.Windows;
using Forms = System.Windows.Forms;

namespace OpenPackager;
public partial class MainWindow : Window
{
    string? root; string? csproj; string? lastOutput;
    public MainWindow() { InitializeComponent(); }
    static string LogPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "GITHUB", "OpenPackager.log");
    static void Log(string message) { try { Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!); File.AppendAllText(LogPath, $"{DateTime.UtcNow:O}  {message}{Environment.NewLine}"); } catch { } }
    void ChooseFolder_Click(object sender, RoutedEventArgs e) { using var d = new Forms.FolderBrowserDialog { Description = "Choose the project folder to package" }; if (d.ShowDialog() == Forms.DialogResult.OK) { root = d.SelectedPath; ProjectPath.Text = root; Log("Folder selected: " + root); Scan(); } }
    void Scan_Click(object sender, RoutedEventArgs e) => Scan();
    void Scan() { if (string.IsNullOrWhiteSpace(ProjectPath.Text) || !Directory.Exists(ProjectPath.Text)) { HealthText.Text = "Choose a valid project folder first."; Log("Scan rejected: invalid folder"); return; } root = ProjectPath.Text; csproj = Directory.EnumerateFiles(root, "*.csproj").FirstOrDefault(); var py = File.Exists(Path.Combine(root, "pyproject.toml")); var node = File.Exists(Path.Combine(root, "package.json")); var kind = csproj is not null ? ".NET project" : py ? "Python project" : node ? "Node project" : "Unknown project"; ProjectType.Text = kind; HealthText.Text = csproj is not null ? "Ready to package. Build artifacts and checksums will be generated." : "Detection works, but this adapter is not available in this release."; Activity.Text = $"Scanned {DateTime.Now:T}\nType: {kind}\nSource remains local."; Log($"Scan completed: {kind}"); }
    void OpenOutput_Click(object sender, RoutedEventArgs e) { if (lastOutput is not null && Directory.Exists(lastOutput)) Process.Start(new ProcessStartInfo("explorer.exe", $"\"{lastOutput}\"") { UseShellExecute = true }); }
    async void Build_Click(object sender, RoutedEventArgs e)
    {
        if (csproj is null) Scan();
        if (csproj is null || root is null) return;
        var github = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "GITHUB");
        Directory.CreateDirectory(github);
        var next = Directory.EnumerateDirectories(github).Select(Path.GetFileName).Where(n => n is not null && int.TryParse(n, out _)).Select(n => int.Parse(n!)).DefaultIfEmpty(0).Max() + 1;
        var folder = Path.Combine(github, next.ToString("D3"));
        Directory.CreateDirectory(folder);
        lastOutput = folder; OpenOutput.IsEnabled = false; BuildProgress.Visibility = Visibility.Visible; BuildProgress.IsIndeterminate = true; Activity.Text = $"Building update {next:D3}...\nThis can take a moment.";
        var runtime = ((System.Windows.Controls.ComboBoxItem)Runtime.SelectedItem)?.Tag?.ToString() ?? "win-x64";
        Log($"Build started: runtime={runtime}, selfContained={SelfContained.IsChecked == true}, singleFile={SingleFile.IsChecked == true}");
        var psi = new ProcessStartInfo("dotnet", $"publish \"{csproj}\" -c Release -r {runtime} --self-contained {SelfContained.IsChecked == true} -o \"{folder}\" -p:PublishSingleFile={SingleFile.IsChecked == true} -p:DebugType=None") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        using var p = Process.Start(psi);
        if (p is null) { BuildProgress.IsIndeterminate = false; BuildProgress.Visibility = Visibility.Collapsed; Activity.Text = "Could not start the .NET publisher."; return; }
        await p.WaitForExitAsync();
        if (p.ExitCode != 0) { BuildProgress.IsIndeterminate = false; BuildProgress.Visibility = Visibility.Collapsed; var error = await p.StandardError.ReadToEndAsync(); Log("Build failed: exit=" + p.ExitCode); Activity.Text = "Build failed.\n\n" + error; return; }
        var exe = Directory.EnumerateFiles(folder, "*.exe").FirstOrDefault();
        if (exe is null) { BuildProgress.IsIndeterminate = false; BuildProgress.Visibility = Visibility.Collapsed; Activity.Text = "Build finished, but no executable was produced."; return; }
        var hash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(exe))).ToLowerInvariant();
        await File.WriteAllTextAsync(Path.Combine(folder, "SHA256SUMS.txt"), $"{hash}  {Path.GetFileName(exe)}\n");
        var manifest = new { product = "OpenPackager", update = next, createdUtc = DateTime.UtcNow, project = Path.GetFileNameWithoutExtension(csproj), runtime, selfContained = SelfContained.IsChecked == true, singleFile = SingleFile.IsChecked == true, executable = Path.GetFileName(exe), sha256 = hash };
        await File.WriteAllTextAsync(Path.Combine(folder, "openpackager-manifest.json"), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        var zip = folder.TrimEnd(Path.DirectorySeparatorChar) + ".zip";
        if (File.Exists(zip)) File.Delete(zip);
        ZipFile.CreateFromDirectory(folder, zip, CompressionLevel.Optimal, false);
        Log($"Build completed: update={next:D3}, output={folder}, package={zip}");
        BuildProgress.IsIndeterminate = false; BuildProgress.Value = 100; OpenOutput.IsEnabled = true; Activity.Text = $"Release complete.\n\nUpdate: {next:D3}\nSaved to:\n{folder}\nPackage:\n{zip}\n\n{Path.GetFileName(exe)}";
        HealthText.Text = "Release ready to run.";
    }
}
