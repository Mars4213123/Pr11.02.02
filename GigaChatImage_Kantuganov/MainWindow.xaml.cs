using Microsoft.Win32;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GigaChatImage_Kantuganov
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string ClientId = "7879c628-132f-4ec9-b371-309d6472aa56";
        private const string AuthorizationKey = "Nzg3OWM2MjgtMTMyZi00ZWM5LWIzNzEtMzA5ZDY0NzJhYTU2OmI2ZmM2MDdjLWY2MGEtNDhmYi04MDQ2LTY3Y2U0NzNlMDA1NA==";

        private string currentImagePath = null;
        private string accessToken = null;

        public MainWindow()
        {
            InitializeComponent();

            StyleComboBox.SelectionChanged += ParametersChanged;
            ColorPaletteComboBox.SelectionChanged += ParametersChanged;
            AspectRatioComboBox.SelectionChanged += ParametersChanged;
            QualityComboBox.SelectionChanged += ParametersChanged;
            AddDetailsCheckBox.Checked += ParametersChanged;
            AddDetailsCheckBox.Unchecked += ParametersChanged;
            AddLightingCheckBox.Checked += ParametersChanged;
            AddLightingCheckBox.Unchecked += ParametersChanged;
            HighResCheckBox.Checked += ParametersChanged;
            HighResCheckBox.Unchecked += ParametersChanged;
            PromptTextBox.TextChanged += ParametersChanged;

            CopyPromptButton.Click += CopyPromptButton_Click;

            UpdateGeneratedPrompt();

            _ = InitializeToken();
        }

        private async Task InitializeToken()
        {
            StatusTextBlock.Text = "Получение токена...";
            ProgressBar.IsIndeterminate = true;

            accessToken = await GetToken(ClientId, AuthorizationKey);

            if (accessToken != null)
            {
                StatusTextBlock.Text = "Токен получен. Готов к генерации!";
                GenerateButton.IsEnabled = true;
            }
            else
            {
                StatusTextBlock.Text = "Не удалось получить токен. Проверьте соединение.";
                GenerateButton.IsEnabled = false;
            }

            ProgressBar.IsIndeterminate = false;
        }

        private void ParametersChanged(object sender, RoutedEventArgs e)
        {
            UpdateGeneratedPrompt();
        }

        private void UpdateGeneratedPrompt()
        {
            string basePrompt = PromptTextBox.Text;
            string style = ((ComboBoxItem)StyleComboBox.SelectedItem)?.Tag?.ToString();
            string palette = ((ComboBoxItem)ColorPaletteComboBox.SelectedItem)?.Tag?.ToString();
            string aspectRatio = ((ComboBoxItem)AspectRatioComboBox.SelectedItem)?.Tag?.ToString();
            string quality = ((ComboBoxItem)QualityComboBox.SelectedItem)?.Tag?.ToString();

            StringBuilder enhancedPrompt = new StringBuilder();
            enhancedPrompt.Append(basePrompt);

            if (!string.IsNullOrEmpty(style))
            {
                string styleText = GetStyleDescription(style);
                if (!string.IsNullOrEmpty(styleText))
                {
                    enhancedPrompt.Append($", {styleText}");
                }
            }

            if (!string.IsNullOrEmpty(palette))
            {
                string paletteText = GetPaletteDescription(palette);
                if (!string.IsNullOrEmpty(paletteText))
                {
                    enhancedPrompt.Append($", {paletteText}");
                }
            }

            if (AddDetailsCheckBox.IsChecked == true)
            {
                enhancedPrompt.Append(", детализированное");
            }

            if (AddLightingCheckBox.IsChecked == true)
            {
                enhancedPrompt.Append(", драматическое освещение");
            }

            if (HighResCheckBox.IsChecked == true)
            {
                enhancedPrompt.Append(", высокое разрешение, 4K");
            }

            if (!string.IsNullOrEmpty(aspectRatio))
            {
                enhancedPrompt.Append($", соотношение сторон {aspectRatio}");
            }

            GeneratedPromptTextBox.Text = enhancedPrompt.ToString();
        }

        private string GetStyleDescription(string styleTag)
        {
            return styleTag switch
            {
                "realistic" => "реалистичный стиль",
                "painting" => "в стиле масляной живописи",
                "anime" => "в стиле аниме",
                "cyberpunk" => "в стиле киберпанк",
                "fantasy" => "в стиле фэнтези",
                "minimalism" => "минималистичный стиль",
                "surreal" => "сюрреалистичный стиль",
                "pixelart" => "в стиле пиксель-арт",
                _ => string.Empty
            };
        }

        private string GetPaletteDescription(string paletteTag)
        {
            return paletteTag switch
            {
                "bright" => "яркие насыщенные цвета",
                "pastel" => "пастельные тона",
                "monochrome" => "монохромная цветовая гамма",
                "dark" => "тёмная цветовая тема",
                "warm" => "тёплая цветовая гамма",
                "cool" => "холодная цветовая гамма",
                "neon" => "неоновая цветовая гамма",
                "natural" => "естественные природные цвета",
                _ => string.Empty
            };
        }

        private void CopyPromptButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(GeneratedPromptTextBox.Text);
            StatusTextBlock.Text = "Промпт скопирован в буфер обмена!";
        }

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PromptTextBox.Text))
            {
                MessageBox.Show("Введите описание для генерации изображения", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (accessToken == null)
            {
                MessageBox.Show("Токен не получен. Проверьте соединение и попробуйте снова.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                SetUiState(false);
                StatusTextBlock.Text = "Генерация изображения...";
                ProgressBar.IsIndeterminate = true;

                string enhancedPrompt = GeneratedPromptTextBox.Text;
                string imageFileId = await GenerateImage(accessToken, enhancedPrompt);

                if (imageFileId == null)
                {
                    StatusTextBlock.Text = "Не удалось сгенерировать изображение";
                    MessageBox.Show("Не удалось сгенерировать изображение. Попробуйте другой запрос.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                StatusTextBlock.Text = "Загрузка изображения...";
                currentImagePath = await DownloadImage(accessToken, imageFileId);

                if (currentImagePath == null || !File.Exists(currentImagePath))
                {
                    StatusTextBlock.Text = "Не удалось загрузить изображение";
                    MessageBox.Show("Не удалось загрузить изображение. Проверьте соединение.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                StatusTextBlock.Text = "Изображение готово!";

                SetWallpaperButton.IsEnabled = true;
                SaveButton.IsEnabled = true;

            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Ошибка: {ex.Message}";
                MessageBox.Show($"Произошла ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ProgressBar.IsIndeterminate = false;
                SetUiState(true);
            }
        }

        private void SetUiState(bool isEnabled)
        {
            GenerateButton.IsEnabled = isEnabled;
            PromptTextBox.IsEnabled = isEnabled;
            StyleComboBox.IsEnabled = isEnabled;
            ColorPaletteComboBox.IsEnabled = isEnabled;
            AspectRatioComboBox.IsEnabled = isEnabled;
            QualityComboBox.IsEnabled = isEnabled;
            AddDetailsCheckBox.IsEnabled = isEnabled;
            AddLightingCheckBox.IsEnabled = isEnabled;
            HighResCheckBox.IsEnabled = isEnabled;
            CopyPromptButton.IsEnabled = isEnabled;
        }



        private void SetWallpaperButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentImagePath) || !File.Exists(currentImagePath))
            {
                MessageBox.Show("Изображение не найдено", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                bool wallpaperSet = WallpaperHelper.SetWallpaper(currentImagePath);

                if (wallpaperSet)
                {
                    StatusTextBlock.Text = "Обои успешно установлены!";
                    MessageBox.Show("Обои успешно установлены!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    StatusTextBlock.Text = "Не удалось установить обои";
                    MessageBox.Show("Не удалось установить обои", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при установке обоев: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentImagePath) || !File.Exists(currentImagePath))
            {
                MessageBox.Show("Изображение не найдено", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "JPEG Image|*.jpg|PNG Image|*.png|BMP Image|*.bmp|All files|*.*",
                    FileName = $"wallpaper_{DateTime.Now:yyyyMMdd_HHmmss}",
                    DefaultExt = ".jpg"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    File.Copy(currentImagePath, saveDialog.FileName, true);
                    StatusTextBlock.Text = $"Изображение сохранено: {saveDialog.FileName}";
                    MessageBox.Show($"Изображение сохранено в:\n{saveDialog.FileName}", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static async Task<string> GetToken(string rpUID, string bearer)
        {
            try
            {
                string returnToken = null;
                string uri = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";

                using (HttpClientHandler handler = new HttpClientHandler())
                {
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;

                    using (HttpClient client = new HttpClient(handler))
                    {
                        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);

                        request.Headers.Add("Accept", "application/json");
                        request.Headers.Add("Authorization", $"Bearer {bearer}");
                        request.Headers.Add("RqUID", rpUID);

                        var formData = new List<KeyValuePair<string, string>>
                        {
                            new KeyValuePair<string, string>("scope", "GIGACHAT_API_PERS")
                        };

                        request.Content = new FormUrlEncodedContent(formData);

                        HttpResponseMessage response = await client.SendAsync(request);

                        if (response.IsSuccessStatusCode)
                        {
                            string responseContent = await response.Content.ReadAsStringAsync();
                            var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseContent);
                            returnToken = tokenResponse?.access_token;
                        }
                    }
                }

                return returnToken;
            }
            catch
            {
                return null;
            }
        }

        public static async Task<string> GenerateImage(string accessToken, string prompt)
        {
            try
            {
                string uri = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";

                using (HttpClientHandler handler = new HttpClientHandler())
                {
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;

                    using (HttpClient client = new HttpClient(handler))
                    {
                        client.Timeout = TimeSpan.FromSeconds(120);

                        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);

                        request.Headers.Add("Accept", "application/json");
                        request.Headers.Add("Authorization", $"Bearer {accessToken}");

                        var requestBody = new
                        {
                            model = "GigaChat",
                            messages = new[]
                            {
                                new
                                {
                                    role = "system",
                                    content = "Ты - профессиональный художник. Создавай красивые и детализированные изображения в высоком качестве. Учитывай все детали запроса."
                                },
                                new
                                {
                                    role = "user",
                                    content = prompt
                                }
                            },
                            function_call = "auto"
                        };

                        string jsonBody = JsonConvert.SerializeObject(requestBody);
                        request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                        HttpResponseMessage response = await client.SendAsync(request);

                        if (response.IsSuccessStatusCode)
                        {
                            string responseContent = await response.Content.ReadAsStringAsync();
                            var chatResponse = JsonConvert.DeserializeObject<ChatCompletionResponse>(responseContent);

                            if (chatResponse?.choices != null && chatResponse.choices.Count > 0)
                            {
                                string content = chatResponse.choices[0]?.message?.content;

                                if (!string.IsNullOrEmpty(content))
                                {
                                    var match = Regex.Match(content, @"<img\s+src=""([^""]+)""\s+fuse=""true""\s*/>");

                                    if (match.Success && match.Groups.Count > 1)
                                    {
                                        return match.Groups[1].Value;
                                    }
                                }
                            }
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public static async Task<string> DownloadImage(string accessToken, string fileId)
        {
            try
            {
                string uri = $"https://gigachat.devices.sberbank.ru/api/v1/files/{fileId}/content";
                string fileName = $"gigachat_wallpaper_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                string downloadPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

                using (HttpClientHandler handler = new HttpClientHandler())
                {
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;

                    using (HttpClient client = new HttpClient(handler))
                    {
                        client.Timeout = TimeSpan.FromSeconds(60);

                        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

                        request.Headers.Add("Accept", "application/jpg");
                        request.Headers.Add("Authorization", $"Bearer {accessToken}");

                        HttpResponseMessage response = await client.SendAsync(request);

                        if (response.IsSuccessStatusCode)
                        {
                            byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();

                            if (imageBytes.Length == 0)
                            {
                                return null;
                            }

                            var contentType = response.Content.Headers.ContentType?.MediaType;
                            string extension = ".jpg";

                            if (contentType != null)
                            {
                                if (contentType.ToLower() == "image/png")
                                {
                                    extension = ".png";
                                }
                                else if (contentType.ToLower() == "image/jpeg" || contentType.ToLower() == "image/jpg")
                                {
                                    extension = ".jpg";
                                }
                                else if (contentType.ToLower() == "image/bmp")
                                {
                                    extension = ".bmp";
                                }
                                else if (contentType.ToLower() == "image/webp")
                                {
                                    extension = ".webp";
                                }
                            }

                            if (!fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                            {
                                fileName = System.IO.Path.GetFileNameWithoutExtension(fileName) + extension;
                                downloadPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                            }

                            File.WriteAllBytes(downloadPath, imageBytes);

                            if (File.Exists(downloadPath))
                            {
                                return downloadPath;
                            }
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }

    public class TokenResponse
    {
        public string access_token { get; set; }
        public string expired_at { get; set; }
    }

    public class ChatCompletionResponse
    {
        public List<Choice> choices { get; set; }
        public long created { get; set; }
        public string model { get; set; }
        public string @object { get; set; }
        public Usage usage { get; set; }
    }

    public class Choice
    {
        public Message message { get; set; }
        public int index { get; set; }
        public string finish_reason { get; set; }
    }

    public class Message
    {
        public string content { get; set; }
        public string role { get; set; }
    }

    public class Usage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
    }
}