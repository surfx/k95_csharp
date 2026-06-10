using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using HidSharp;
using Newtonsoft.Json;

namespace K95Controller;

public partial class MainWindow : Window
{
    private string ConfigFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
    private CancellationTokenSource? _cts;
    private HidStream? _stream;

    public class ConfigData
    {
        public Dictionary<string, string> Scripts { get; set; } = new();
        public double? Left { get; set; }
        public double? Top { get; set; }
        public double? Width { get; set; }
        public double? Height { get; set; }
        public WindowState WindowState { get; set; } = WindowState.Normal;
    }

    public MainWindow()
    {
        InitializeComponent();
        LoadConfig();
        StartMonitoring();
    }

    private void LoadConfig()
    {
        if (!File.Exists(ConfigFilePath)) return;

        try
        {
            var json = File.ReadAllText(ConfigFilePath);
            var config = JsonConvert.DeserializeObject<ConfigData>(json);
            if (config == null) return;

            // Restore Scripts
            if (config.Scripts.TryGetValue("G1", out var g1)) TxtG1.Text = g1;
            if (config.Scripts.TryGetValue("G2", out var g2)) TxtG2.Text = g2;
            if (config.Scripts.TryGetValue("G3", out var g3)) TxtG3.Text = g3;
            if (config.Scripts.TryGetValue("G4", out var g4)) TxtG4.Text = g4;
            if (config.Scripts.TryGetValue("G5", out var g5)) TxtG5.Text = g5;
            if (config.Scripts.TryGetValue("G6", out var g6)) TxtG6.Text = g6;

            // Restore Window Position
            if (config.Left.HasValue && config.Top.HasValue) 
                Position = new PixelPoint((int)config.Left.Value, (int)config.Top.Value);
            
            if (config.Width.HasValue) Width = config.Width.Value;
            if (config.Height.HasValue) Height = config.Height.Value;
            WindowState = config.WindowState == WindowState.Minimized ? WindowState.Normal : config.WindowState;
        }
        catch (Exception ex)
        {
            Log($"Erro ao carregar config: {ex.Message}");
        }
    }

    private void SaveConfig()
    {
        var config = new ConfigData
        {
            Scripts = new Dictionary<string, string>
            {
                { "G1", TxtG1.Text ?? "" },
                { "G2", TxtG2.Text ?? "" },
                { "G3", TxtG3.Text ?? "" },
                { "G4", TxtG4.Text ?? "" },
                { "G5", TxtG5.Text ?? "" },
                { "G6", TxtG6.Text ?? "" }
            },
            Left = Position.X,
            Top = Position.Y,
            Width = Width,
            Height = Height,
            WindowState = WindowState
        };

        File.WriteAllText(ConfigFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
    }

    private async void BrowseScript_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag?.ToString() is not string targetName) return;
        
        var textBox = this.FindControl<TextBox>(targetName);
        if (textBox == null) return;

        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Selecionar Script",
            FileTypeFilter = new[] 
            { 
                new FilePickerFileType("Scripts") { Patterns = new[] { "*.ps1", "*.sh", "*.py" } },
                FilePickerFileTypes.All 
            },
            AllowMultiple = false
        });

        if (files.Count > 0)
        {
            textBox.Text = files[0].Path.LocalPath;
            SaveConfig();
            Log($"Configurado {btn.Name?.Replace("Btn", "")}: {Path.GetFileName(textBox.Text)}");
        }
    }

    private void ClearLogs_Click(object sender, RoutedEventArgs e) => TxtLog.Clear();

    private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

    private void OnTitleBarPointerPressed(object sender, PointerPressedEventArgs e) => BeginMoveDrag(e);

    private void Log(string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            TxtLog.Text += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
            TxtLog.CaretIndex = TxtLog.Text?.Length ?? 0;
        });
    }

    private void StartMonitoring()
    {
        _cts = new CancellationTokenSource();
        Task.Run(() => MonitorLoop(_cts.Token));
    }

    private void MonitorLoop(CancellationToken token)
    {
        const int vendorId = 0x1B1C;
        const int productId = 0x1B2D;
        const string targetPathPart = "col03";

        while (!token.IsCancellationRequested)
        {
            try
            {
                var device = DeviceList.Local.GetHidDevices(vendorId, productId)
                    .FirstOrDefault(d => d.DevicePath.ToLower().Contains(targetPathPart));

                if (device == null)
                {
                    Log("Teclado não detectado. Recomentando em 5s...");
                    Thread.Sleep(5000);
                    continue;
                }

                if (device.TryOpen(out _stream))
                {
                    using (_stream)
                    {
                        Log("Conectado ao K95!");
                        var buf = new byte[64];

                        while (!token.IsCancellationRequested)
                        {
                            try
                            {
                                int res = _stream.Read(buf);
                                if (res > 0 && buf[0] == 0x03 && res > 16 && buf[16] != 0x00)
                                {
                                    HandleKeyPress(buf[16]);
                                }
                            }
                            catch (TimeoutException) { continue; }
                            catch (IOException ex) when (ex.Message.Contains("timeout")) { continue; }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Erro: {ex.Message}");
                Thread.Sleep(2000);
            }
        }
    }

    private void HandleKeyPress(byte keyByte)
    {
        string? gKey = keyByte switch
        {
            0x01 => "G1", 0x02 => "G2", 0x04 => "G3",
            0x08 => "G4", 0x10 => "G5", 0x20 => "G6",
            _ => null
        };

        if (gKey == null) return;

        Dispatcher.UIThread.Post(() =>
        {
            var scriptPath = gKey switch
            {
                "G1" => TxtG1.Text, "G2" => TxtG2.Text, "G3" => TxtG3.Text,
                "G4" => TxtG4.Text, "G5" => TxtG5.Text, "G6" => TxtG6.Text,
                _ => null
            };

            Log($"Tecla {gKey} detectada.");
            
            if (!string.IsNullOrWhiteSpace(scriptPath))
            {
                Log($"Executando: {Path.GetFileName(scriptPath)}");
                RunScript(scriptPath);
            }
            else
            {
                Log($"Aviso: Nenhum script para {gKey}.");
            }
        });
    }

    private void RunScript(string scriptPath)
    {
        try
        {
            if (!File.Exists(scriptPath))
            {
                Log($"ERRO: Arquivo não encontrado: {scriptPath}");
                return;
            }

            var (fileName, arguments) = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
                ? ("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"")
                : scriptPath.EndsWith(".ps1") 
                    ? ("pwsh", $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"")
                    : ("/bin/bash", $"\"{scriptPath}\"");

            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };

            var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

            process.OutputDataReceived += (_, e) => { if (e.Data != null) Log($"> {e.Data}"); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) Log($"ERR> {e.Data}"); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.Exited += (_, _) => { Log("Finalizado."); process.Dispose(); };
        }
        catch (Exception ex)
        {
            Log($"Falha ao iniciar: {ex.Message}");
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        SaveConfig();
        _cts?.Cancel();
        _stream?.Dispose();
        base.OnClosing(e);
    }
}
