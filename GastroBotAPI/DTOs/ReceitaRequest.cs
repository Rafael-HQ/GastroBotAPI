namespace WebApplication1.DTOs
{
    public class ReceitaRequest
    {
        public List<string> Ingredientes { get; set; } = new();
    }

    public class ReceitaResponse
    {
        public string Receita { get; set; }
    }
}