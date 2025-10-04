using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using WebApplication1.DTOs;

namespace GastroBotAPI.Services;

public class GeminiServices
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public GeminiServices(HttpClient httpClient, IConfiguration config)
    {
        _http = httpClient;
        _apiKey = config["GeminiApiKey"]
                  ?? throw new InvalidOperationException("Gemini API Key não configurada");
    }

    public async Task<ReceitaResponse?> GerarReceita(List<string> ingredientes)
    {
        if (ingredientes == null || ingredientes.Count == 0)
            throw new ArgumentException("Informe ao menos 1 ingrediente.");
        
        string prompt = $"eu tenho esses ingredientes: {string.Join(", ", ingredientes)}. Monte para mim uma receita com esses ingredientes e retorne nesse formato:\n\nconst receita = {{\n  nome: \"Bife Acebolado com Tomate\",\n  ingredientes: [\"...\"],\n  modoPreparo: [\"...\"],\n}};\n\nRetorne apenas o que eu te pedi, sem coisas extra.";
        
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
        
        var response = await _http.PostAsync(
            $"v1beta/models/gemini-2.0-flash:generateContent?key={_apiKey}", 
            content);
        
        var responseString = await response.Content.ReadAsStringAsync();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n--- Resposta da Gemini ---\n");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine(responseString);
        Console.ResetColor();
        
        var result = JObject.Parse(responseString);
        string? texto =
            result["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString()
            ?? result["candidates"]?[0]?["output_text"]?.ToString();

        if (string.IsNullOrEmpty(texto))
            return null;

        return ParsearRespostaGemini(texto);
        
    }
    
    private ReceitaResponse ParsearRespostaGemini(string textoResposta)
    {
        var response = new ReceitaResponse();

        try
        {
            var nomeMatch = Regex.Match(textoResposta, @"nome:\s*""([^""]*)""");
            if (nomeMatch.Success)
                response.Titulo = nomeMatch.Groups[1].Value;

            var ingredientesMatch = Regex.Match(textoResposta, @"ingredientes:\s*\[(.*?)\]", RegexOptions.Singleline);
            if (ingredientesMatch.Success)
                response.Ingredientes = string.Join("\n", ExtrairItensLista(ingredientesMatch.Groups[1].Value));

            var modoPreparoMatch = Regex.Match(textoResposta, @"modoPreparo:\s*\[(.*?)\]", RegexOptions.Singleline);
            if (modoPreparoMatch.Success)
                response.ModoPreparo = string.Join("\n", ExtrairItensLista(modoPreparoMatch.Groups[1].Value));
        }
        catch
        {
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
            if (match.Success) itens.Add(match.Groups[1].Value.Trim());

        return itens;
    }

    
}