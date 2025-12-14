using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace APIGigaChatImage_Kantuganov
{
    public class Program
    {
        static string ClientId = "7879c628-132f-4ec9-b371-309d6472aa56";
        static string AuthorizationKey = "Nzg3OWM2MjgtMTMyZi00ZWM5LWIzNzEtMzA5ZDY0NzJhYTU2OjBiYTVlZjRhLTFiYzgtNDIxMi1iMzk0LWZjN2VmODlkNzNhOA==";

        static async Task Main(string[] args)
        {

            Console.Write("Введите описание для изображения: ");
            string userPrompt = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userPrompt))
            {
                userPrompt = "Красивый пейзаж с горами и озером при закате";
            }

            string accessToken = await GetToken(ClientId, AuthorizationKey);

            if (accessToken == null)
            {
                Console.WriteLine("Не удалось получить токен. Использую стандартные обои.");
                UseDefaultWallpaper();
                WaitForExit();
                return;
            }

            string imageFileId = await GenerateImage(accessToken, userPrompt);

            if (imageFileId == null)
            {
                Console.WriteLine("Не удалось сгенерировать изображение. Использую стандартные обои.");
                UseDefaultWallpaper();
                WaitForExit();
                return;
            }

            string downloadedImagePath = await DownloadImage(accessToken, imageFileId);

            if (downloadedImagePath == null || !File.Exists(downloadedImagePath))
            {
                Console.WriteLine("Не удалось скачать изображение. Использую стандартные обои.");
                UseDefaultWallpaper();
                WaitForExit();
                return;
            }

            bool wallpaperSet = Wallpapersetter.SetWallpaper(downloadedImagePath);

            if (wallpaperSet)
            {
                Console.WriteLine("Обои успешно установлены!");
            }
            else
            {
                Console.WriteLine("Не удалось установить обои. Использую стандартные обои.");
                UseDefaultWallpaper();
            }

            WaitForExit();
        }

        private static void WaitForExit()
        {
            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        private static void UseDefaultWallpaper()
        {
            try
            {
                string defaultWallpaper = @"C:\Users\student-A502.PERMAVIAT\Pictures\Снимок.png";

                if (File.Exists(defaultWallpaper))
                {
                    Wallpapersetter.SetWallpaper(defaultWallpaper);
                }
            }
            catch { }
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
                                    content = "Ты - профессиональный художник. Создавай красивые и детализированные изображения в высоком качестве."
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
                string downloadPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

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
                                fileName = Path.GetFileNameWithoutExtension(fileName) + extension;
                                downloadPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
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

        public class Wallpapersetter
        {
            private const int SPI_SETDESKWALLPAPER = 0x0014;
            private const int SPIF_UPDATEINIFILE = 0x01;
            private const int SPIF_SENDWININICHANGE = 0x02;

            [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
            private static extern int SystemParametersInfo(
                int uAction,
                int uParam,
                string lpvParam,
                int fuWinIni
            );

            public static bool SetWallpaper(string imagePath)
            {
                try
                {
                    if (!File.Exists(imagePath))
                    {
                        return false;
                    }

                    int result = SystemParametersInfo(
                        SPI_SETDESKWALLPAPER,
                        0,
                        imagePath,
                        SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE
                    );

                    return result != 0;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}