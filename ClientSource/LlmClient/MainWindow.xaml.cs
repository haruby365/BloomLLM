// © 2022 Jong-il Hong

using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace Haruby.LlmClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly DependencyProperty InputPromptProperty = DependencyProperty.Register(
            nameof(InputPrompt), typeof(string), typeof(MainWindow), new PropertyMetadata("This is input string and"));
        public static readonly DependencyProperty OutputMessageProperty = DependencyProperty.Register(
            nameof(OutputMessage), typeof(string), typeof(MainWindow), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IsUsingInputSeedProperty = DependencyProperty.Register(
            nameof(IsUsingInputSeed), typeof(bool), typeof(MainWindow));
        public static readonly DependencyProperty InputSeedProperty = DependencyProperty.Register(
            nameof(InputSeed), typeof(int), typeof(MainWindow));
        public static readonly DependencyProperty InputMaxNewTokensProperty = DependencyProperty.Register(
            nameof(InputMaxNewTokens), typeof(int), typeof(MainWindow), new PropertyMetadata(20));

        public static readonly DependencyProperty OutputSeedProperty = DependencyProperty.Register(
            nameof(OutputSeed), typeof(int), typeof(MainWindow));
        public static readonly DependencyProperty OutputMaxNewTokensProperty = DependencyProperty.Register(
            nameof(OutputMaxNewTokens), typeof(int), typeof(MainWindow));

        public string InputPrompt { get => (string)GetValue(InputPromptProperty); set => SetValue(InputPromptProperty, value); }
        public string OutputMessage { get => (string)GetValue(OutputMessageProperty); set => SetValue(OutputMessageProperty, value); }

        public bool IsUsingInputSeed { get => (bool)GetValue(IsUsingInputSeedProperty); set => SetValue(IsUsingInputSeedProperty, value); }
        public int InputSeed { get => (int)GetValue(InputSeedProperty); set => SetValue(InputSeedProperty, value); }
        public int InputMaxNewTokens { get => (int)GetValue(InputMaxNewTokensProperty); set => SetValue(InputMaxNewTokensProperty, value); }

        public int OutputSeed { get => (int)GetValue(OutputSeedProperty); set => SetValue(OutputSeedProperty, value); }
        public int OutputMaxNewTokens { get => (int)GetValue(OutputMaxNewTokensProperty); set => SetValue(OutputMaxNewTokensProperty, value); }

        HttpClient? httpClient;

        OutputPacket? lastOutput;

        readonly Random random = new();

        public MainWindow()
        {
            InitializeComponent();

            InputSeed = random.Next();

            httpClient = new()
            {
                BaseAddress = new Uri("http://localhost:" + Port),
            };
        }

        protected override void OnClosed(EventArgs e)
        {
            if (httpClient is not null)
            {
                httpClient.Dispose();
                httpClient = null;
            }
            base.OnClosed(e);
        }

        private void NextSeed()
        {
            InputSeed = random.Next();
        }

        // https://huggingface.co/docs/transformers/v4.23.1/en/main_classes/text_generation#transformers.generation_utils.GenerationMixin.generate
        private void GenerateJob(HttpClient client, string prompt, int seed, int maxNewTokens)
        {
            try
            {
                string inputJson = JsonSerializer.Serialize(new InputPacket(prompt, seed, maxNewTokens));
                
                using HttpResponseMessage response = client.PostAsync((Uri?)null, new StringContent(inputJson), CancellationToken.None).Result;
                string output = response.Content.ReadAsStringAsync().Result;
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        {
                            OutputPacket outputPacket = JsonSerializer.Deserialize<OutputPacket>(output) ?? throw new InvalidOperationException("Server response is null JSON.");
                            Dispatcher.Invoke(() =>
                            {
                                lastOutput = outputPacket;
                                OutputSeed = outputPacket.Seed;
                                OutputMaxNewTokens = outputPacket.MaxNewTokens;
                                OutputMessage = outputPacket.Message;
                            });
                        }
                        break;

                    default:
                        MessageBox.Show("Communication Error.\n\n" + output, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unexpected error occured.\n\n" + ex, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            finally
            {
                Dispatcher.Invoke(() => GenerateButton.IsEnabled = true);
            }
        }

        private void NextSeedButton_Click(object sender, RoutedEventArgs e)
        {
            NextSeed();
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (httpClient is null)
            {
                return;
            }

            GenerateButton.IsEnabled = false;

            HttpClient client = httpClient;
            string prompt = InputPrompt;
            int seed = IsUsingInputSeed ? InputSeed : random.Next();
            int maxNewTokens = InputMaxNewTokens;
            Task.Run(() => GenerateJob(client, prompt, seed, maxNewTokens));
        }

        private void SendToInputButton_Click(object sender, RoutedEventArgs e)
        {
            InputPrompt = OutputMessage;
        }

        private void SaveJob(DateTime timeStamp, OutputPacket target)
        {
            try
            {
                string saveDirPath = Path.GetFullPath(SaveDirName);
                Directory.CreateDirectory(saveDirPath);

                string filename = DateTimeToFileName(timeStamp);
                File.WriteAllText(Path.Combine(saveDirPath, filename + ".txt"), target.Message);

                using Stream jsonFile = File.Create(Path.Combine(saveDirPath, filename + ".json"));
                JsonSerializer.Serialize(jsonFile, target);

                // Fake delay
                Thread.Sleep(200);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unexpected error occured.\n\n" + ex, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            finally
            {
                Dispatcher.Invoke(() =>
                {
                    Cursor = Cursors.Arrow;
                    SaveButton.IsEnabled = true;
                });
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (lastOutput is null)
            {
                MessageBox.Show("Please generate first.", "Save", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SaveButton.IsEnabled = false;
            Cursor = Cursors.AppStarting;

            DateTime timeStamp = DateTime.Now;
            OutputPacket target = lastOutput;
            Task.Run(() => SaveJob(timeStamp, target));
        }

        static string DateTimeToFileName(DateTime dateTime)
        {
            return string.Format("{0}-{1}-{2}_{3}-{4}-{5}",
                dateTime.Year.ToString("0000"), dateTime.Month.ToString("00"), dateTime.Day.ToString("00"),
                dateTime.Hour.ToString("00"), dateTime.Minute.ToString("00"), dateTime.Second.ToString("00"));
        }

        public const string SaveDirName = "Saves";
        public const ushort Port = 62040;
    }
}
