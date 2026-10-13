<#
.SYNOPSIS
    Gera o pacote de validação Windows x64 do CapParse (self-contained, single-file).

.DESCRIPTION
    Procedimento de publicação reproduzível para os pacotes beta (M15). Etapas:

      1. dotnet restore
      2. dotnet build  -c <Configuration>
      3. dotnet test   -c <Configuration>
      4. dotnet publish (win-x64, self-contained, single-file, nativas embutidas)
      5. Remoção dos modelos v5 do pacote RapidOcrNet que o aplicativo não usa
      6. Inspeção da saída de publish e criação do ZIP versionado
      7. Registro do tamanho e do SHA-256 do pacote

    A versão é lida de <Version> em src/CapParse/CapParse.csproj (fonte única
    de verdade para o nome do pacote). O layout do artefato (um único
    CapParse.exe + pasta models/) é o mesmo do v0.1.0-beta.1, cujo ZIP foi
    gerado de forma ad hoc — este script torna o procedimento reproduzível.

    O publish usa PublishSingleFile + IncludeNativeLibrariesForSelfExtract:
    o executável embute o runtime .NET e as dependências nativas
    (onnxruntime, SkiaSharp). Essa é a mesma configuração usada e validada
    em campo no v0.1.0-beta.1.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File build\publish-win-x64.ps1
    powershell -ExecutionPolicy Bypass -File build\publish-win-x64.ps1 -Configuration Release
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [string]$Runtime = 'win-x64'
)

$ErrorActionPreference = 'Stop'

$repoRoot   = Split-Path -Parent $PSScriptRoot
$slnPath    = Join-Path $repoRoot 'CapParse.sln'
$csproj     = Join-Path $repoRoot 'src\CapParse\CapParse.csproj'
$distRoot   = Join-Path $repoRoot 'build\dist'
$publishDir = Join-Path $distRoot "CapParse-$Runtime"

function Invoke-DotNet([string[]]$arguments) {
    Write-Host ">> dotnet $($arguments -join ' ')"
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($arguments[0]) falhou com código de saída $LASTEXITCODE"
    }
}

# --- Versão (fonte única: <Version> no csproj) -----------------------------
[xml]$projXml = Get-Content -Raw $csproj
# Select-Xml evita a colisão do PowerShell: "$pg.Version" resolveria a
# propriedade .NET XmlDocument.Version (string), não o elemento filho.
$versionNode = Select-Xml -Xml $projXml -XPath '//PropertyGroup/Version' | Select-Object -First 1
if ($null -eq $versionNode -or [string]::IsNullOrWhiteSpace($versionNode.Node.InnerText)) {
    throw "Propriedade <Version> não encontrada em $csproj"
}
$version = $versionNode.Node.InnerText
Write-Host "Versão candidata: $version"

# --- 1. Restore -------------------------------------------------------------
Invoke-DotNet @('restore', $slnPath)

# --- 2. Build ----------------------------------------------------------------
Invoke-DotNet @('build', $slnPath, '-c', $Configuration, '--no-restore')

# --- 3. Testes (incluem os smoke tests com o modelo real PP-OCRv6) ----------
Invoke-DotNet @('test', $slnPath, '-c', $Configuration, '--no-restore')

# --- 4. Publish ---------------------------------------------------------------
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
Invoke-DotNet @(
    'publish', $csproj, '-c', $Configuration, '-r', $Runtime,
    '--self-contained', 'true',
    '-p:PublishSingleFile=true',
    '-p:IncludeNativeLibrariesForSelfExtract=true',
    '-o', $publishDir
)

# --- 5. Modelos v5 não utilizados ---------------------------------------------
# O pacote RapidOcrNet 4.2.0 copia o conjunto completo models/v5 para a saída
# de publish. O preset PPOCRv6Small (único preset usado pelo app) referencia
# apenas:
#   models/v6/PP-OCRv6_det_small.onnx
#   models/v6/PP-OCRv6_rec_small.onnx
#   models/v6/ppocrv6_dict.txt
#   models/v5/ch_PP-LCNet_x0_25_textline_ori_cls_mobile.onnx  (classificador)
# Os demais três arquivos v5 pertencem ao preset PP-OCRv5 mobile, que o
# aplicativo nunca carrega; removê-los do pacote é seguro e explícito.
$unusedV5Models = @(
    'ch_PP-OCRv5_mobile_det.onnx',
    'latin_PP-OCRv5_rec_mobile_infer.onnx',
    'ppocrv5_latin_dict.txt'
)
foreach ($name in $unusedV5Models) {
    $file = Join-Path $publishDir "models\v5\$name"
    if (Test-Path $file) {
        Remove-Item $file -Force
        Write-Host "Removido (não utilizado pelo preset v6): models\v5\$name"
    }
}

# --- 6. Inspeção e empacotamento ---------------------------------------------
$requiredFiles = @(
    'CapParse.exe',
    'models\v5\ch_PP-LCNet_x0_25_textline_ori_cls_mobile.onnx',
    'models\v6\PP-OCRv6_det_small.onnx',
    'models\v6\PP-OCRv6_rec_small.onnx',
    'models\v6\ppocrv6_dict.txt'
)
foreach ($rel in $requiredFiles) {
    $path = Join-Path $publishDir $rel
    if (-not (Test-Path $path)) {
        throw "Arquivo esperado ausente da saída de publish: $rel"
    }
}
Write-Host "Saída de publish verificada: $($requiredFiles.Count) arquivos obrigatórios presentes."

$zipName = "CapParse-v$version-$Runtime.zip"
$zipPath = Join-Path $distRoot $zipName
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

# Lista de arquivos explícita: o ZIP contém apenas o executável e os modelos
# necessários. PDB, bibliotecas .lib de linkagem e qualquer outro artefato de
# desenvolvimento ficam de fora (mesmo conteúdo que o beta.1).
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::Open($zipPath, 'Create')
try {
    foreach ($rel in $requiredFiles) {
        $fullPath = Join-Path $publishDir $rel
        # Nomes de entrada com barra reversa, como no ZIP do v0.1.0-beta.1.
        $entry = $zip.CreateEntry($rel, [System.IO.Compression.CompressionLevel]::Optimal)
        $entryStream = $entry.Open()
        try {
            $fileStream = [System.IO.File]::OpenRead($fullPath)
            try { $fileStream.CopyTo($entryStream) }
            finally { $fileStream.Dispose() }
        }
        finally { $entryStream.Dispose() }
    }
}
finally { $zip.Dispose() }

# --- 7. Registro do artefato ---------------------------------------------------
$zipItem = Get-Item $zipPath
$sha256 = (Get-FileHash $zipPath -Algorithm SHA256).Hash
Write-Host ''
Write-Host '=== Pacote gerado ==='
Write-Host "Caminho: $zipPath"
Write-Host ('Tamanho: {0:N0} bytes ({1:N1} MB)' -f $zipItem.Length, ($zipItem.Length / 1MB))
Write-Host "SHA-256: $sha256"
