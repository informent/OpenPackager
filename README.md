# OpenPackager

OpenPackager is a privacy-first, open-source Windows release builder for developers who want a professional packaging workflow without a subscription.

## First release

- Detects .NET, Python, and Node projects.
- Validates the project before packaging.
- Publishes .NET applications with configurable self-contained and single-file options.
- Assembles a clean release directory.
- Creates SHA-256 checksums and a JSON build report.
- Never uploads source code or uses telemetry.
- Automatically places each desktop build in the next numbered `Downloads\\GITHUB` update folder.
- Writes a release manifest and SHA-256 checksum beside every executable.
- Creates a compressed ZIP package automatically after a successful build.

## Usage

```powershell
dotnet run -- detect C:\path\to\project
dotnet run -- validate C:\path\to\project
dotnet run -- package C:\path\to\project --output .\dist --runtime win-x64 --self-contained --single-file
```

The tool is intentionally deterministic: every package contains `openpackager-manifest.json` and `SHA256SUMS.txt`.

## License

MIT. See [LICENSE](LICENSE).
