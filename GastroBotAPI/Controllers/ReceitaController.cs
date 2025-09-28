using WebApplication1.DTOs;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReceitaController : ControllerBase
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _endpoint;

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

            string prompt = $"eu tenho esses ingredientes: {string.Join(", ", request.Ingredientes)}. Monte para mim uma receita com esses ingredientes e retorne nesse formato:\n \nconst receita = {{\n  nome: \"Bife Acebolado com Tomate\",\n  ingredientes: [\n    \"4 bifes (de alcatra, contrafilé ou filé mignon)\",\n    \"1 cebola grande fatiada\",\n    \"2 dentes de alho picados\",\n    \"2 tomates maduros picados\",\n    \"Sal e pimenta-do-reino a gosto\",\n  ],\n  modoPreparo: [\n    \"Tempere os bifes com o alho picado, sal e pimenta-do-reino a gosto.\",\n    \"Aqueça o azeite e grelhe os bifes até o ponto desejado.\",\n    \"Refogue a cebola na mesma frigideira.\",\n    \"Acrescente os tomates e cozinhe por 3-5 minutos.\",\n    \"Volte os bifes e envolva no molho.\",\n    \"Sirva imediatamente.\",\n  ],\n}};\n \nRetorne apenas o que eu te pedi, sem coisas extra";

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

            // Parsear a resposta para extrair os campos individuais
            var receitaResponse = ParsearRespostaGemini(texto);
            
            return Ok(receitaResponse);
        }

        private ReceitaResponse ParsearRespostaGemini(string textoResposta)
        {
            var response = new ReceitaResponse();
            
            try
            {
                // Extrair nome/título
                var nomeMatch = Regex.Match(textoResposta, @"nome:\s*""([^""]*)""");
                if (nomeMatch.Success)
                {
                    response.Titulo = nomeMatch.Groups[1].Value;
                }

                // Extrair lista de ingredientes
                var ingredientesMatch = Regex.Match(textoResposta, @"ingredientes:\s*\[(.*?)\]", RegexOptions.Singleline);
                if (ingredientesMatch.Success)
                {
                    var ingredientesContent = ingredientesMatch.Groups[1].Value;
                    var ingredientesList = ExtrairItensLista(ingredientesContent);
                    response.Ingredientes = string.Join("\n", ingredientesList);
                }

                // Extrair modo de preparo
                var modoPreparoMatch = Regex.Match(textoResposta, @"modoPreparo:\s*\[(.*?)\]", RegexOptions.Singleline);
                if (modoPreparoMatch.Success)
                {
                    var modoPreparoContent = modoPreparoMatch.Groups[1].Value;
                    var modoPreparoList = ExtrairItensLista(modoPreparoContent);
                    response.ModoPreparo = string.Join("\n", modoPreparoList);
                }
            }
            catch (Exception ex)
            {
                // Em caso de erro no parsing, define valores padrão
                response.Titulo = "Receita Gerada";
                response.Ingredientes = "Não foi possível extrair os ingredientes";
                response.ModoPreparo = "Não foi possível extrair o modo de preparo";
            }

            return response;
        }

        private List<string> ExtrairItensLista(string content)
        {
            var itens = new List<string>();
            var matches = Regex.Matches(content, @"""([^""]*)""");
            
            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    itens.Add(match.Groups[1].Value.Trim());
                }
            }
            
            return itens;
        }
    }
}