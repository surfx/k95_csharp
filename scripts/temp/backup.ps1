[Console]::InputEncoding  = [System.Text.Encoding]::UTF8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
Clear-Host

$path_script = $PSScriptRoot
Set-Location $path_script

# Configurações de Caminho
$sourceBase = "C:\Users\Emerson\AppData\Local\ReadyOrNot\Saved"
$archivePath = "D:\backup\googledrive\readyornot.7z"
$password = "root"
$sevenZipExe = "C:\Program Files\7-Zip\7z.exe"

# Itens específicos para incluir no backup
$itemsToBackup = @("Config", "SaveGames", "ReadyOrNot_PCD3D_SM6.upipelinecache")

# 1. Verificação de integridade
if (!(Test-Path $sourceBase)) {
    Write-Host "Erro: Pasta de origem não encontrada em $sourceBase" -ForegroundColor Red
    exit
}

# Garante que a pasta de destino do backup existe
$destFolder = Split-Path $archivePath
if (!(Test-Path $destFolder)) { New-Item -ItemType Directory -Path $destFolder -Force }

# 2. Execução do Backup via 7-Zip
Write-Host "Iniciando backup em $archivePath..." -ForegroundColor Cyan

# Remove o backup antigo se existir para criar um do zero
if (Test-Path $archivePath) { Remove-Item $archivePath -Force }

# Coleta todos os itens que existem de fato
$existingItems = @()
foreach ($item in $itemsToBackup) {
    $fullPath = Join-Path $sourceBase $item
    if (Test-Path $fullPath) {
        $existingItems += "`"$fullPath`""
    } else {
        Write-Host "Aviso: Item $item não encontrado, pulando..." -ForegroundColor Gray
    }
}

if ($existingItems.Count -gt 0) {
    Write-Host "Comprimindo $($existingItems.Count) itens..." -ForegroundColor Yellow
    # Executa o 7-Zip uma única vez com todos os itens
    $itemsArg = $existingItems -join " "
    Invoke-Expression "& `"$sevenZipExe`" a `"$archivePath`" $itemsArg -p$password -y"
} else {
    Write-Host "Erro: Nenhum item encontrado para backup." -ForegroundColor Red
}

Write-Host "`nBackup concluído com sucesso!" -ForegroundColor Green
Set-Location $path_script
