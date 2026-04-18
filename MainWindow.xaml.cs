using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HidSharp;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace K95Controller
{
    public partial class MainWindow : Window
    {
        private string ConfigFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        private CancellationTokenSource? _cts;
        private HidStream? _stream;

        public MainWindow()
        {
            InitializeComponent();
            LoadConfig();
            StartMonitoring();
        }

        private void LoadConfig()
        {
            if (File.Exists(ConfigFilePath))
            {
                try
                {
                    var json = File.ReadAllText(ConfigFilePath);
                    var config = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    if (config != null)
                    {
                        if (config.ContainsKey("G1")) TxtG1.Text = config["G1"];
                        if (config.ContainsKey("G2")) TxtG2.Text = config["G2"];
                        if (config.ContainsKey("G3")) TxtG3.Text = config["G3"];
                        if (config.ContainsKey("G4")) TxtG4.Text = config["G4"];
                        if (config.ContainsKey("G5")) TxtG5.Text = config["G5"];
                        if (config.ContainsKey("G6")) TxtG6.Text = config["G6"];
                    }
                }
                catch (Exception ex)
                {
                    Log($"Erro ao carregar config: {ex.Message}");
                }
            }
        }

        private void SaveConfig()
        {
            var config = new Dictionary<string, string>
            {
                { "G1", TxtG1.Text },
                { "G2", TxtG2.Text },
                { "G3", TxtG3.Text },
                { "G4", TxtG4.Text },
                { "G5", TxtG5.Text },
                { "G6", TxtG6.Text }
            };
            File.WriteAllText(ConfigFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
        }

        private void BrowseScript_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            var targetName = btn.Tag.ToString();
            var textBox = (TextBox)this.FindName(targetName!);

            var openFileDialog = new OpenFileDialog
            {
                Filter = "PowerShell Scripts (*.ps1)|*.ps1|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                textBox.Text = openFileDialog.FileName;
                SaveConfig();
                Log($"Script configurado para {btn.Name.Replace("Btn", "")}: {openFileDialog.FileName}");
            }
        }

        private void ClearLogs_Click(object sender, RoutedEventArgs e)
        {
            TxtLog.Clear();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Log(string message)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                TxtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
                TxtLog.ScrollToEnd();
            }));
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
                    var loader = DeviceList.Local;
                    var device = loader.GetHidDevices(vendorId, productId)
                        .FirstOrDefault(d => d.DevicePath.ToLower().Contains(targetPathPart));

                    if (device == null)
                    {
                        Log("Teclado K95 (Col03) não encontrado. Tentando novamente em 5s...");
                        Thread.Sleep(5000);
                        continue;
                    }

                    if (device.TryOpen(out _stream))
                    {
                        using (_stream)
                        {
                            _stream.ReadTimeout = 10000;
                            Log("Conectado ao K95! Monitorando teclas...");
                            byte[] buf = new byte[64];

                            while (!token.IsCancellationRequested)
                            {
                                try
                                {
                                    int res = _stream.Read(buf);
                                    if (res > 0 && buf[0] == 0x03 && res > 16)
                                    {
                                        byte keyByte = buf[16];
                                        if (keyByte != 0x00)
                                        {
                                            HandleKeyPress(keyByte);
                                        }
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
                    Log($"Erro no monitor: {ex.Message}");
                    Thread.Sleep(2000);
                }
            }
        }

        private void HandleKeyPress(byte keyByte)
        {
            string gKey = "";
            string scriptPath = "";

            Dispatcher.Invoke(() =>
            {
                switch (keyByte)
                {
                    case 0x01: gKey = "G1"; scriptPath = TxtG1.Text; break;
                    case 0x02: gKey = "G2"; scriptPath = TxtG2.Text; break;
                    case 0x04: gKey = "G3"; scriptPath = TxtG3.Text; break;
                    case 0x08: gKey = "G4"; scriptPath = TxtG4.Text; break;
                    case 0x10: gKey = "G5"; scriptPath = TxtG5.Text; break;
                    case 0x20: gKey = "G6"; scriptPath = TxtG6.Text; break;
                }
            });

            if (!string.IsNullOrEmpty(gKey))
            {
                Log($"Tecla {gKey} pressionada.");
                if (!string.IsNullOrEmpty(scriptPath))
                {
                    Log($"Executando: {scriptPath}");
                    RunPowerShellScript(scriptPath);
                }
                else
                {
                    Log($"Nenhum script configurado para {gKey}.");
                }
            }
        }

        private void RunPowerShellScript(string scriptPath)
        {
            try
            {
                if (!File.Exists(scriptPath))
                {
                    Log($"ERRO: Arquivo não encontrado: {scriptPath}");
                    return;
                }

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    // No Windows BR, o PowerShell fala CP1252 por padrão no console.
                    // O .NET captura isso corretamente com Encoding.Default.
                    StandardOutputEncoding = System.Text.Encoding.Default,
                    StandardErrorEncoding = System.Text.Encoding.Default
                };

                Process? process = new Process { StartInfo = psi, EnableRaisingEvents = true };

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data)) Log($"> {e.Data}");
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data)) Log($"ERR> {e.Data}");
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.Exited += (sender, e) =>
                {
                    Log("Script finalizado.");
                    process.Dispose();
                };
            }
            catch (Exception ex)
            {
                Log($"Falha ao iniciar processo: {ex.Message}");
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            _cts?.Cancel();
            _stream?.Dispose();
            base.OnClosed(e);
        }
    }
}
