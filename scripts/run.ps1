[Console]::InputEncoding  = [System.Text.Encoding]::UTF8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$scriptPath = $PSScriptRoot
$projectRoot = Split-Path $scriptPath -Parent
$projectFile = Join-Path $projectRoot "K95Controller.csproj"

# Chama o kill usando o caminho absoluto
& "$scriptPath\kill.ps1"

if (!(Test-Path $projectFile)) {
    Write-Host "Erro: Arquivo do projeto não encontrado em: $projectFile" -ForegroundColor Red
    exit
}

Write-Host "Iniciando o projeto C#..." -ForegroundColor Green
dotnet run --project "$projectFile"
