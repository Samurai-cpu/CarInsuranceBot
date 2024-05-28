using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using CarInsuranceBot.Models.Mindee;
using CarInsuranceBot.Models;
using CarInsuranceBot.Intefaces;
using System.Net;

namespace CarInsuranceBot.Services
{
    public class MindeeService : IMindeeService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfigurationSection _configuration;

        public MindeeService(HttpClient httpClient, IConfiguration configuration) 
        {
            _httpClient = httpClient;
            _configuration = configuration.GetSection("Mindee");

            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Token", _configuration["apiKey"]);
        }

        /// <summary>
        /// Returns extracted Car's Title data from mindee API.
        /// </summary>
        /// <param name="base64string">Phto in base 64 encoding</param>
        /// <returns>Car's title data</returns>
        public async Task<Title> ParseTitleImageAsync(string base64string)
        {
            var response = await PostImageToMindeeAsync(base64string, _configuration["titlePostUrl"]!);
            return await GetRusltFromJobAsync<Title>($"{_configuration["titleGetUrl"]}/{response!.Job.Id}");
        }

        /// <summary>
        /// Returns extracted Passport data from mindee API.
        /// </summary>
        /// <param name="base64string">Phto in base 64 encoding</param>
        /// <returns>Cutomer's passport data</returns>
        public async Task<Passport> ParsePassportImageAsync(string base64string)
        {
            var response = await PostImageToMindeeAsync(base64string, _configuration["passportPostUrl"]!);
            return await GetRusltFromJobAsync<Passport>($"{_configuration["passportGetUrl"]!}/{response!.Job.Id}");
        }

        /// <summary>
        /// Generic method for post requests to mindee API
        /// </summary>
        /// <param name="base64Image">Photo in base 64 encoding</param>
        /// <param name="url">Endpoint url</param>
        /// <returns>Response of mindee's post endpoint</returns>
        /// <exception cref="Exception"></exception>
        private async Task<PostResponse?> PostImageToMindeeAsync(string base64Image, string url)
        {
            var str = JsonConvert.SerializeObject(new { document = base64Image });
            var content = new StringContent(str, Encoding.UTF8, "application/json");
            
            HttpResponseMessage response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadFromJsonAsync<PostResponse>();
            if (response.IsSuccessStatusCode)
            {       
                return responseContent;
            }
            else
            {
                throw new Exception($"Exception ocured during request to Mindee API :{response.StatusCode},{responseContent?.RequestStatus?.Error?.Details}, {responseContent?.RequestStatus?.Error?.Message}");
            }
        }

        /// <summary>
        /// Generic method which retrieves result of processing uploaded image
        /// </summary>
        /// <typeparam name="T">Generic parameter</typeparam>
        /// <param name="url">Endpoint url</param>
        /// <returns>Photo parsing result</returns>
        /// <exception cref="Exception"></exception>
        private async Task<T?> GetRusltFromJobAsync<T>(string url)
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            if(response.StatusCode == HttpStatusCode.Found)
            {
                return await HandleRedirect<T>(response);
            }
            else if(response.StatusCode == HttpStatusCode.OK)
            {
                //The 200 status code from get endpoint indicates the proccesing state of uploaded image,
                //that's why we wait before retry
                await Task.Delay(1500);
                return await GetRusltFromJobAsync<T>(url);
            }
            else
            {
                throw new Exception($" Exception ocured during request to Mindee API :{response.StatusCode}");
            }
        }

        /// <summary>
        /// Calls endpoint from redirection url
        /// </summary>
        /// <typeparam name="T">Genric parameter</typeparam>
        /// <param name="response">Get endpoint Mindee response with extracted data from image</param>
        /// <returns></returns>
        private async Task<T?> HandleRedirect<T>(HttpResponseMessage response)
        {
            string redirectUrl = response.Headers.Location!.ToString();
            var rediretResponse = await _httpClient.GetAsync(redirectUrl);
            rediretResponse.EnsureSuccessStatusCode();
            var getRequestResponse = await rediretResponse.Content.ReadFromJsonAsync<GetResponse<T>>();
            return getRequestResponse!.Document!.Inference!.Prediction;
        }

    }
}