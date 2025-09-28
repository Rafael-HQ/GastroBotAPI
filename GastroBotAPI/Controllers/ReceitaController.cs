using WebApplication1.DTOs;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReceitaController : ControllerBase
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _endpoint;

        // APENAS UM construtor - remova o outro!
        public ReceitaController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _http = httpClientFactory.CreateClient();
            _apiKey = configuration["GeminiApiKey"] 
                      ?? throw new InvalidOperationException("Gemini API Key não configurada");
            _endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_apiKey}";
        }

        [HttpPost("GerarReceita")]
        public async Task<ActionResult<ReceitaResponse>> GerarReceita([FromBody] ReceitaRequest request)
        {
            if (request.Ingredientes == null || request.Ingredientes.Count == 0)
                return BadRequest("Informe ao menos 1 ingrediente.");

            string prompt = $"Crie uma receita deliciosa usando estes ingredientes: {string.Join(", ", request.Ingredientes)}.";

            var requestBody = new
            {
                contents = new object[]
                {
                    new {
                        parts = new object[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(_endpoint, content);
            var responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine("\n--- Resposta da Gemini ---\n");
            Console.WriteLine(responseString);
            
            var result = JObject.Parse(responseString);
            string? texto =
                result["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString()
                ?? result["candidates"]?[0]?["output_text"]?.ToString();
            
            if (string.IsNullOrEmpty(texto))
                return StatusCode(500, "Não consegui gerar receita.");

            return Ok(new ReceitaResponse { Receita = texto });
        }
    }
}

