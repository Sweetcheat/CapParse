# CapParse

Local-first Windows desktop utility: capture a region of your screen and turn its visual content into usable text and images. OCR runs fully locally, with no account and no cloud dependency.

The repository is in early development. Product, UI, architecture and development documents are in [`docs/`](docs/).

## Repository layout

```text
CapParse.sln
src/CapParse        WPF application
tests/CapParse.Tests xUnit tests
docs/               Product, UI, architecture and development documents
assets/             Binary assets (placeholder)
.github/            GitHub configuration (placeholder)
```

## Building

Requires the .NET 8 SDK (or a newer SDK that can target `net8.0` / `net8.0-windows`).

```powershell
dotnet build
```

## Running tests

```powershell
dotnet test
```

## License

MIT — see [LICENSE](LICENSE).