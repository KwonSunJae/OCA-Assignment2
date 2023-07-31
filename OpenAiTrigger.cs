using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
namespace OCA_Assignment2
{
    public class OpenAiTrigger
    {
        private readonly ILogger _logger;

        public OpenAiTrigger(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<OpenAiTrigger>();
        }

        [Function("OpenAiTrigger")]
        [OpenApiOperation(operationId: nameof(OpenAiTrigger.Run), tags: new[] { "name" })]
        [OpenApiRequestBody(contentType: "text/plain", bodyType: typeof(string), Required = true, Description = "The request body")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "completions")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var prompt = req.ReadAsString();
            using var httpClient = new HttpClient();
            var apiKey = Environment.GetEnvironmentVariable("API_KEY");
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            //httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
            var requestBody = JsonSerializer.Serialize(new
            {
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant. You are very good at summarizing the given text into 2-3 bullet points." },
                    new { role = "user", content = prompt }
                },
                model = "gpt-3.5-turbo",
                max_tokens = 800,
                temperature = 0.7f,
            });
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var responseBody = await response.Content.ReadAsStringAsync();
            dynamic responseJson = JsonSerializer.Deserialize<dynamic>(responseBody);
            string message;
            using (JsonDocument doc = JsonDocument.Parse(responseBody))
            {
                // "message" 필드에 접근
                JsonElement messageElement = doc.RootElement.GetProperty("choices")[0].GetProperty("message");

                // "content" 필드의 값을 가져옴
                string contentValue = messageElement.GetProperty("content").GetString();
                message = contentValue;
            }
            var httpResponse = req.CreateResponse(HttpStatusCode.OK);
            httpResponse.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            httpResponse.WriteString(message);
            return httpResponse;
        }
    }

}
