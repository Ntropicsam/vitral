param([switch]$Subir)

$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
Set-Location $raiz
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'
$repositorio = 'https://github.com/Ntropicsam/vitral'

function Paso($texto, [scriptblock]$accion) {
    Write-Host "`n== $texto" -ForegroundColor Cyan
    & $accion
    if ($LASTEXITCODE -ne 0) { throw "Falló: $texto" }
}

$version = ([xml](Get-Content Directory.Build.props)).Project.PropertyGroup.Version
Write-Host "Vitral $version"

Paso 'Editor: dependencias' { npm --prefix editor ci --no-audit --no-fund }
Paso 'Editor: tipos' { npm --prefix editor run typecheck }
Paso 'Editor: pruebas' { npm --prefix editor test }
Paso 'Editor: compilar' { npm --prefix editor run build }
Paso 'Núcleo: pruebas' { dotnet test tests/Vitral.Core.Tests -c Release -nologo -v q }

if (Test-Path publicar) { Remove-Item publicar -Recurse -Force }
Paso 'App' { dotnet publish src/Vitral.App -c Release -r win-x64 --self-contained -o publicar/app -nologo -v q }
Paso 'Servicio de sensores' {
    dotnet publish src/Vitral.Servicio -c Release -r win-x64 --self-contained `
        -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=false `
        -o publicar/app/servicio -nologo -v q
}
Get-ChildItem publicar/app/servicio -Filter *.pdb | Remove-Item
Remove-Item publicar/app/servicio/libSkiaSharp.dll
Get-ChildItem publicar/app -Filter *.pdb | Remove-Item

Paso 'Herramienta vpk' { dotnet tool restore }
if ($Subir) {
    $env:VPK_TOKEN = (gh auth token)
    Paso 'Versiones anteriores' { dotnet vpk download github --repoUrl $repositorio --token $env:VPK_TOKEN --outputDir publicar/instalador }
}
Paso 'Instalador' {
    dotnet vpk pack `
        --packId Vitral `
        --packVersion $version `
        --runtime win-x64 `
        --packDir publicar/app `
        --mainExe Vitral.exe `
        --packTitle Vitral `
        --packAuthors Ntropicsam `
        --icon src/Vitral.App/vitral.ico `
        --outputDir publicar/instalador
}
if ($Subir) {
    Paso 'Subir a GitHub' {
        dotnet vpk upload github --repoUrl $repositorio --token $env:VPK_TOKEN --outputDir publicar/instalador `
            --publish --releaseName "Vitral $version" --tag "v$version"
    }
    Remove-Item Env:VPK_TOKEN
}
Write-Host "`nListo: publicar/instalador" -ForegroundColor Green
