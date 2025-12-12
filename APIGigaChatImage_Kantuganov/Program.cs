using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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

            // Пример использования
            string prompt = "Красивый закат над горами";
            string imageUrl = await GenerateImage(Token, prompt);

            if (imageUrl != null)
            {
                Console.WriteLine($"Изображение успешно сгенерировано: {imageUrl}");
            }
            else
            {
                Console.WriteLine("Ошибка при генерации изображения");
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
    }
}