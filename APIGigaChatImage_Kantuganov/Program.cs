using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace APIGigaChatImage_Kantuganov
{
    public class Program
    {
        static string ClientId = "***";
        static string AuthorizationKey = "***";
        static void Main(string[] args)
        {
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
    }
}
