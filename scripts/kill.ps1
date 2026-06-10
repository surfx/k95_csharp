[Console]::InputEncoding  = [System.Text.Encoding]::UTF8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$processName = "K95Controller"
$processes = Get-Process | Where-Object { $_.ProcessName -eq $processName }

if ($processes) {
    $processIds = $processes.Id
    $processes | Stop-Process -Force -ErrorAction SilentlyContinue
    # Aguarda o encerramento real para liberar handles de arquivo
    Wait-Process -Id $processIds -ErrorAction SilentlyContinue -Timeout 2
    Write-Host "Processo '$processName' encerrado com sucesso." -ForegroundColor Yellow
} else {
    Write-Host "Nenhum processo '$processName' em execução." -ForegroundColor Gray
}
