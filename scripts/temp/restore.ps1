[Console]::InputEncoding  = [System.Text.Encoding]::UTF8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
Clear-Host

$path_script = $PSScriptRoot
Set-Location $path_script

# Configurações de Caminho
$archivePath = "D:\backup\googledrive\readyornot.7z"
$password = "root"
$destBase = "C:\Users\Emerson\AppData\Local\ReadyOrNot\Saved"
$tempDir = Join-Path $env:TEMP "RoN_Extraction"

# Itens específicos para extrair
$itemsToExtract = @("Config", "SaveGames", "ReadyOrNot_PCD3D_SM6.upipelinecache")

# 1. Preparação
if (!(Test-Path $destBase)) { New-Item -ItemType Directory -Path $destBase -Force }
if (Test-Path $tempDir) { Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue }
New-Item -ItemType Directory -Path $tempDir -Force

# 2. Extração Seletiva
foreach ($item in $itemsToExtract) {
    & "C:\Program Files\7-Zip\7z.exe" x "$archivePath" "-p$password" "-o$tempDir" "$item" -y
}

# 3. Movimentação e Sobrescrita Manual (Garante substituição total)
foreach ($item in $itemsToExtract) {
    $source = Join-Path $tempDir $item
    $destination = Join-Path $destBase $item
    
    if (Test-Path $source) {
        Write-Host "Restaurando $item..." -ForegroundColor Cyan
        
        # Se o destino já existe (arquivo ou pasta), removemos antes para evitar conflitos
        if (Test-Path $destination) {
            Remove-Item -Path $destination -Recurse -Force
        }
        
        # Move o item extraído para o local definitivo
        Move-Item -Path $source -Destination $destination -Force
    }
}

# 4. Limpeza final
if (Test-Path $tempDir) { Remove-Item $tempDir -Recurse -Force }

Write-Host "`nProcesso concluído com sucesso em $destBase" -ForegroundColor Green
Set-Location $path_script