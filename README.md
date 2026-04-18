# K95 Controller (C# Version)

Este projeto é uma réplica em C# do controlador HID para o teclado Corsair K95, originalmente escrito em Rust.

## Funcionalidades
- Monitora a Interface `Col03` do dispositivo HID (Vendor ID: `0x1B1C`, Product ID: `0x1B2D`).
- Detecta pressões nas teclas G1 a G6.
- Executa scripts PowerShell baseados na tecla pressionada:
  - **G1**: Executa `scripts/temp/restore.ps1`
  - **G2**: Executa `scripts/temp/backup.ps1`
  - **G3-G6**: Exibe uma mensagem no console.

## Como Executar
1. Certifique-se de ter o .NET SDK instalado.
2. Navegue até esta pasta:
   ```powershell
   cd k95_csharp
   ```
3. Execute o projeto:
   ```powershell
   dotnet run
   ```

## Dependências
- [HidSharp](https://www.nuget.org/packages/HidSharp): Usada para comunicação com dispositivos HID de forma multiplataforma.

## Notas
- O código procura por um dispositivo que contenha "col03" no caminho do dispositivo. Se não encontrar, ele listará todos os dispositivos Corsair detectados para ajudar no diagnóstico.
- Os scripts PowerShell são executados com `-ExecutionPolicy Bypass`.
