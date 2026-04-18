[Console]::InputEncoding  = [System.Text.Encoding]::UTF8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$processName = "K95Controller"
$processes = Get-Process | Where-Object { $_.ProcessName -eq $processName }

if ($processes) {
    $processes | Stop-Process -Force -ErrorAction SilentlyContinue
    Write-Host "Processo '$processName' encerrado com sucesso." -ForegroundColor Yellow
} else {
    Write-Host "Nenhum processo '$processName' em execução." -ForegroundColor Gray
}
