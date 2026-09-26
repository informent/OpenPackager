using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using Forms = System.Windows.Forms;

namespace OpenPackager;
public partial class MainWindow : Window
{
    string? root; string? csproj;
    public MainWindow() { InitializeComponent(); }
    void ChooseFolder_Click(object sender, RoutedEventArgs e) { using var d = new Forms.FolderBrowserDialog { Description = "Choose the project folder to package" }; if (d.ShowDialog() == Forms.DialogResult.OK) { root = d.SelectedPath; ProjectPath.Text = root; Scan(); } }
    void Scan_Click(object sender, RoutedEventArgs e) => Scan();
    void Scan() { if (string.IsNullOrWhiteSpace(ProjectPath.Text) || !Directory.Exists(ProjectPath.Text)) { HealthText.Text = "Choose a valid project folder first."; return; } root = ProjectPath.Text; csproj = Directory.EnumerateFiles(root, "*.csproj").FirstOrDefault(); var py = File.Exists(Path.Combine(root, "pyproject.toml")); var node = File.Exists(Path.Combine(root, "package.json")); var kind = csproj is not null ? ".NET project" : py ? "Python project" : node ? "Node project" : "Unknown project"; ProjectType.Text = kind; HealthText.Text = csproj is not null ? "Ready to package. Build artifacts and checksums will be generated." : "Detection works, but this adapter is not available in this release."; Activity.Text = $"Scanned {DateTime.Now:T}\nType: {kind}\nSource remains local."; }
    async void Build_Click(object sender, RoutedEventArgs e) { if (csproj is null) Scan(); if (csproj is null || root is null) return; var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "GITHUB", "002"); Directory.CreateDirectory(folder); Activity.Text = "Building release...\nThis can take a moment."; var psi = new ProcessStartInfo("dotnet", $"publish \"{csproj}\" -c Release -r win-x64 --self-contained {SelfContained.IsChecked == true} -o \"{folder}\" -p:PublishSingleFile={SingleFile.IsChecked == true} -p:DebugType=None") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true }; using var p = Process.Start(psi)!; await p.WaitForExitAsync(); if (p.ExitCode != 0) { Activity.Text = await p.StandardError.ReadToEndAsync(); return; } var exe = Directory.EnumerateFiles(folder, "*.exe").FirstOrDefault(); if (exe is not null) await File.WriteAllTextAsync(Path.Combine(folder, "SHA256SUMS.txt"), $"{Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(exe))).ToLowerInvariant()}  {Path.GetFileName(exe)}\n"); Activity.Text = $"Release complete.\n\nSaved to:\n{folder}\n\n{(exe is null ? "No executable found." : Path.GetFileName(exe))}"; HealthText.Text = "Release ready to run."; }
}
