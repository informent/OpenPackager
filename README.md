# OpenPackager

OpenPackager is a privacy-first, open-source Windows release builder for developers who want a professional packaging workflow without a subscription.

## Current workflow

- Detects .NET, Python, and Node projects.
- Detects supported project markers before packaging.
- Publishes .NET applications with configurable self-contained and single-file options.
- Assembles a clean release directory.
- Creates SHA-256 checksums and a JSON build report.
- Never uploads source code or uses telemetry.
- Automatically places each desktop build in the next numbered `Downloads\\GITHUB` update folder.
- Writes a release manifest and SHA-256 checksum beside every executable.
- Creates a compressed ZIP package automatically after a successful build.

## Usage

Start `OpenPackager.exe`, choose your project folder, select the runtime and packaging options, then select **Build release**. Completed packages are placed in numbered folders under `Downloads\GITHUB`, with a ZIP beside the folder.

.NET projects are published using your installed .NET SDK. Python and Node projects produce source bundles; building standalone Python or Node executables is a separate workflow using the scripts under `tools`. Review source bundles before distributing them because project files can contain private configuration.

Each completed package contains `openpackager-manifest.json` and `SHA256SUMS.txt`. Checksums cover the manifest and every payload file, excluding the checksum file itself. Repeating checksum generation without changing the payload produces the same checksum list. ZIP files contain timestamps and are not claimed to be byte-for-byte reproducible.

Version 2.9.0 streams checksum calculation, preserves existing ZIPs, removes unfinished ZIPs after failures, and recovers from publisher errors without leaving Build disabled. Packaging the release folder rejects symbolic links and junctions. This is a local packaging tool, not a sandbox for untrusted project build scripts.

## Verification

```powershell
dotnet run --project tests/OpenPackager.EngineTests.csproj -c Release
dotnet publish OpenPackager.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o test-output
powershell -NoProfile -File tests/PackageUi.ps1 -Exe "$PWD/test-output/OpenPackager.exe"
```

The packaged-app test needs an interactive Windows desktop. It creates a fixture project, builds a source ZIP through the actual window, verifies archived hashes, and checks invalid-folder and compiler-error recovery. GitHub Actions runs the engine and packaged-app tests on Windows. See [VALIDATION.md](VALIDATION.md) for the verification scope.

Portable packages include installer scripts under `installer`. Code signing is supported through `tools\Sign-Release.ps1` when a valid certificate thumbprint is supplied; unsigned builds are never presented as signed.

## License

MIT. See [LICENSE](LICENSE).
