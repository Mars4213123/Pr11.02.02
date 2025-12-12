using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using APIGigaChatImage_Kantuganov.Models.Response;
using Newtonsoft.Json;

namespace APIGigaChatImage_Kantuganov
{
    public class Program
    {
        static string ClientId = "***";
        static string AuthorizationKey = "***";

        static async Task Main(string[] args)
        {
            string Token = await GetToken(ClientId, AuthorizationKey);

            string prompt = Console.ReadLine();
            string imageUrl = await GenerateImage(Token, prompt);

            if (imageUrl != null)
            {
                Console.WriteLine($"Изображение успешно сгенерировано: {imageUrl}");

                string imageId = ExtractImageIdFromUrl(imageUrl);

                if (!string.IsNullOrEmpty(imageId))
                {
                    string filePath = await DownloadImage(Token, imageId, "generated_image.jpg");

                    if (filePath != null)
                    {
                        Console.WriteLine($"Изображение сохранено: {filePath}");
                    }
                    else
                    {
                        Console.WriteLine("Ошибка при скачивании изображения");
                    }
                }
                else
                {
                    Console.WriteLine("Не удалось извлечь идентификатор изображения из URL");
                }
            }
            else
            {
                Console.WriteLine("Ошибка при генерации изображения");
            }
        }

        private static string ExtractImageIdFromUrl(string imageUrl)
        {
            try
            {
                Uri uri = new Uri(imageUrl);
                string lastSegment = uri.Segments.Last();

                if (lastSegment.Contains('.'))
                {
                    lastSegment = lastSegment.Substring(0, lastSegment.LastIndexOf('.'));
                }

                return lastSegment;
            }
            catch
            {
                return null;
            }
        }

        public static async Task<string> GetToken(string rpUID, string bearer)
        {
            string ReturnToken = null;
            string Uri = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";

            using (HttpClientHandler Handler = new HttpClientHandler())
            {
                Handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;

                using (HttpClient Client = new HttpClient(Handler))
                {
                    HttpRequestMessage Request = new HttpRequestMessage(HttpMethod.Post, Uri);

                    Request.Headers.Add("Accept", "application/json");
                    Request.Headers.Add("Authorization", $"bearer {bearer}");

                    var Data = new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("scope", "GIGANET_API_PERS")
                    };

                    Request.Content = new FormUrlEncodedContent(Data);

                    HttpResponseMessage Response = await Client.SendAsync(Request);

                    if (Response.IsSuccessStatusCode)
                    {
                        string ResponseContent = await Response.Content.ReadAsStringAsync();
                        ResponseToken Token = JsonConvert.DeserializeObject<ResponseToken>(ResponseContent);
                        ReturnToken = Token.access_token;
                    }
                }
            }

            return ReturnToken;
        }

        public static async Task<string> GenerateImage(string token, string prompt)
        {
            string imageUrl = null;
            string uri = "https://gigachat.devices.sberbank.ru/api/v1/images/generations";

            using (HttpClientHandler handler = new HttpClientHandler())
            {
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;

                using (HttpClient client = new HttpClient(handler))
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);

                    request.Headers.Add("Accept", "application/json");
                    request.Headers.Add("Authorization", $"Bearer {token}");

                    var requestBody = new
                    {
                        model = "GigaChat",
                        prompt = prompt,
                        n = 1,
                        size = "1024x1024"
                    };

                    string jsonBody = JsonConvert.SerializeObject(requestBody);
                    request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                    HttpResponseMessage response = await client.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        var imageResponse = JsonConvert.DeserializeObject<ImageGenerationResponse>(responseContent);

                        if (imageResponse != null && imageResponse.data != null && imageResponse.data.Count > 0)
                        {
                            imageUrl = imageResponse.data[0].url;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка: {response.StatusCode}");
                        string errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Детали ошибки: {errorContent}");
                    }
                }
            }

            return imageUrl;
        }

        public static async Task<string> DownloadImage(string token, string imageId, string fileName = null)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = $"{imageId}.jpg";
                }

                string downloadPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);

                string uri = $"https://gigachat.devices.sberbank.ru/api/v1/images/{imageId}";

                using (HttpClientHandler handler = new HttpClientHandler())
                {
                    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true;

                    using (HttpClient client = new HttpClient(handler))
                    {
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                        Console.WriteLine($"Скачиваю изображение {imageId}...");

                        using (HttpResponseMessage response = await client.GetAsync(uri))
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                using (FileStream fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None))
                                {
                                    await response.Content.CopyToAsync(fileStream);
                                }

                                Console.WriteLine($"Изображение сохранено в: {downloadPath}");
                                return downloadPath;
                            }
                            else
                            {
                                Console.WriteLine($"Ошибка при скачивании: {response.StatusCode}");
                                return null;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Исключение при скачивании изображения: {ex.Message}");
                return null;
            }
        }
    }
}