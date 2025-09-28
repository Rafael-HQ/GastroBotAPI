namespace WebApplication1.DTOs
{
    public class ReceitaRequest
    {
        public List<string> Ingredientes { get; set; } = new();
    }

    public class ReceitaResponse
    {
        public string Titulo { get; set; }
        public string Ingredientes { get; set; }
        public string ModoPreparo { get; set; }
    }
}