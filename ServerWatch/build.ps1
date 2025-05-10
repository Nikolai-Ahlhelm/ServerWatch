# build.ps1

# Abfrage: Produktversion eingeben
$version = Read-Host "Welche Produktversion willst du bauen? (z. B. 1.0.0)"

# Buildnummer automatisch erzeugen: yyDDDHH (z. B. 2513009 = 2025, Tag 130, 09 Uhr)
$now = Get-Date
$year = $now.ToString("yy")
$dayOfYear = $now.DayOfYear.ToString("D3")
$hour = $now.ToString("HH")
$buildNumber = "$year$dayOfYear$hour"

# Kombinierte Version für InformationalVersion
$informationalVersion = "$version+$buildNumber"
Write-Host "👉 Erzeuge Version: $informationalVersion" -ForegroundColor Cyan

# Zielordner für Build
$outputDir = Join-Path -Path "artifacts" -ChildPath "$informationalVersion"
if (!(Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

# Projekt bauen
dotnet publish `
    -c Release `
    -o $outputDir `
    -p:InformationalVersion=$informationalVersion `
    -p:Version=$version `
    -p:AssemblyVersion=$version `
    -p:FileVersion=$version

# version.txt zur Referenz schreiben
Set-Content -Path (Join-Path $outputDir "version.txt") -Value $informationalVersion

# ZIP-Datei erstellen
$zipPath = "$outputDir.zip"
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}
Compress-Archive -Path "$outputDir\*" -DestinationPath $zipPath

Write-Host "`n✅ Build abgeschlossen."
Write-Host "📂 Ordner: $outputDir"
Write-Host "🗜️ ZIP-Datei: $zipPath" -ForegroundColor Green
pause