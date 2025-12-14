using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;

namespace GigaChatImage_Kantuganov
{
    public partial class MainWindow : Window
    {
        private string currentImagePath = null;
        private string accessToken = null;

        private const string AUTHORIZATION_KEY = "Nzg3OWM2MjgtMTMyZi00ZWM5LWIzNzEtMzA5ZDY0NzJhYTU2OjBiYTVlZjRhLTFiYzgtNDIxMi1iMzk0LWZjN2VmODlkNzNhOA==";
        private const string CLIENT_ID = "7879c628-132f-4ec9-b371-309d6472aa56";

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await GetToken();
        }

        private async Task GetToken()
        {
            try
            {
                StatusTextBlock.Text = "Получение токена...";

                using var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                using var client = new HttpClient(handler);

                var request = new HttpRequestMessage(HttpMethod.Post, "https://ngw.devices.sberbank.ru:9443/api/v2/oauth");
                request.Headers.Add("Accept", "application/json");
                request.Headers.Add("Authorization", $"Bearer {AUTHORIZATION_KEY}");
                request.Headers.Add("RqUID", CLIENT_ID);

                var formData = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("scope", "GIGACHAT_API_PERS")
                };

                request.Content = new FormUrlEncodedContent(formData);

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(json);
                    accessToken = tokenResponse?.access_token;

                    StatusTextBlock.Text = "Токен получен!";
                    GenerateButton.IsEnabled = true;
                }
                else
                {
                    StatusTextBlock.Text = "Ошибка API";
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "Ошибка получения токена";
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (accessToken == null)
            {
                MessageBox.Show("Токен не получен");
                return;
            }

            if (string.IsNullOrWhiteSpace(PromptTextBox.Text))
            {
                MessageBox.Show("Введите описание");
                return;
            }

            try
            {
                StatusTextBlock.Text = "Генерация...";
                GenerateButton.IsEnabled = false;

                // Формируем промпт с учетом выбранных параметров
                string basePrompt = PromptTextBox.Text;
                string style = (StyleComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "";
                string palette = (ColorPaletteComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "";
                string ratio = (AspectRatioComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "";

                string fullPrompt = basePrompt;
                if (!string.IsNullOrEmpty(style)) fullPrompt += $", {style} стиль";
                if (!string.IsNullOrEmpty(palette)) fullPrompt += $", {palette}";
                if (!string.IsNullOrEmpty(ratio)) fullPrompt += $", соотношение сторон {ratio}";

                // Генерация изображения
                string imageFileId = await GenerateImage(fullPrompt);

                if (string.IsNullOrEmpty(imageFileId))
                {
                    throw new Exception("Не удалось получить ID изображения");
                }

                StatusTextBlock.Text = "Загрузка...";

                // Скачивание изображения
                currentImagePath = await DownloadImage(imageFileId);

                if (string.IsNullOrEmpty(currentImagePath) || !File.Exists(currentImagePath))
                {
                    throw new Exception("Не удалось сохранить изображение");
                }

                StatusTextBlock.Text = "Готово!";
                SetWallpaperButton.IsEnabled = true;
                MessageBox.Show($"Изображение сохранено: {Path.GetFileName(currentImagePath)}");
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "Ошибка";
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
            finally
            {
                GenerateButton.IsEnabled = true;
            }
        }

        private async Task<string> GenerateImage(string prompt)
        {
            try
            {
                using var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                using var client = new HttpClient(handler);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                // Согласно документации - добавляем function_call: "auto"
                var requestBody = new
                {
                    model = "GigaChat",
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = "Ты - профессиональный художник. Создавай качественные изображения для обоев."
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
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://gigachat.devices.sberbank.ru/api/v1/chat/completions", content);

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Ошибка API: {response.StatusCode}");
                }

                string responseJson = await response.Content.ReadAsStringAsync();
                var chatResponse = JsonConvert.DeserializeObject<ChatCompletionResponse>(responseJson);

                if (chatResponse?.choices != null && chatResponse.choices.Count > 0)
                {
                    string contentText = chatResponse.choices[0]?.message?.content;

                    if (!string.IsNullOrEmpty(contentText))
                    {
                        // Ищем тег <img src="file_id" fuse="true"/>
                        var match = Regex.Match(contentText, @"<img\s+src=""([^""]+)""\s+fuse=""true""\s*/>");
                        if (match.Success)
                        {
                            return match.Groups[1].Value;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка генерации: {ex.Message}");
                return null;
            }
        }

        private async Task<string> DownloadImage(string fileId)
        {
            try
            {
                using var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

                using var client = new HttpClient(handler);
                client.DefaultRequestHeaders.Add("Accept", "image/jpeg");
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                string url = $"https://gigachat.devices.sberbank.ru/api/v1/files/{fileId}/content";

                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Ошибка загрузки: {response.StatusCode}");
                }

                byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();

                if (imageBytes.Length == 0)
                {
                    throw new Exception("Получен пустой файл");
                }

                // Сохраняем в папку программы
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string fileName = $"wallpaper_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                string filePath = Path.Combine(appDir, fileName);

                await File.WriteAllBytesAsync(filePath, imageBytes);

                return filePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
                return null;
            }
        }

        private void SetWallpaperButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentImagePath == null || !File.Exists(currentImagePath))
            {
                MessageBox.Show("Изображение не найдено");
                return;
            }

            try
            {
                bool result = WallpaperHelper.SetWallpaper(currentImagePath);
                if (result)
                {
                    MessageBox.Show("Обои установлены!");
                }
                else
                {
                    MessageBox.Show("Ошибка установки");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void HolidayButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new HolidayWindow();
            if (window.ShowDialog() == true)
            {
                PromptTextBox.Text = window.SelectedPrompt;
            }
        }
    }

    // Модели для JSON десериализации
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