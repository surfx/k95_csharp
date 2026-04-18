[Console]::InputEncoding  = [System.Text.Encoding]::UTF8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$scriptPath = $PSScriptRoot
$projectRoot = Split-Path $scriptPath -Parent
$projectFile = Join-Path $projectRoot "K95Controller.csproj"
$publishDir = Join-Path $projectRoot "publish"

# 1. Encerra o processo se estiver aberto
& "$scriptPath\kill.ps1"

if (!(Test-Path $projectFile)) {
    Write-Host "Erro: Arquivo do projeto não encontrado em: $projectFile" -ForegroundColor Red
    exit
}

# 2. Limpa a pasta de publish antiga
if (Test-Path $publishDir) {
    Write-Host "Limpando pasta de release antiga..." -ForegroundColor Gray
    Remove-Item -Path $publishDir -Recurse -Force
}

# 3. Executa o Publish
Write-Host "Criando Release do projeto C# (Modo Otimizado)..." -ForegroundColor Cyan

# Flags:
# -c Release: Compila com otimizações
# -o $publishDir: Define a pasta de saída
# --self-contained false: Usa o framework instalado no Windows (menor tamanho)
# -p:PublishSingleFile=true: Junta tudo em um único .exe (exceto DLLs nativas se necessário)
# -p:EnableCompressionInSingleFile=true: Comprime o executável
dotnet publish "$projectFile" `
    -c Release `
    -o "$publishDir" `
    --self-contained false `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nRelease criada com sucesso em: $publishDir" -ForegroundColor Green
    # Abre a pasta da release
    explorer.exe $publishDir
} else {
    Write-Host "`nErro ao criar a release." -ForegroundColor Red
}
