using WebApplication1.DTOs;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using GastroBotAPI.Services;
using Microsoft.AspNetCore.Authorization;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReceitaController : ControllerBase
    {
        private readonly GeminiServices _geminiServices;

        public ReceitaController(GeminiServices geminiServices)
        {
            _geminiServices = geminiServices;
        }

        [HttpPost("GerarReceita")]
        public async Task<ActionResult<ReceitaResponse>> GerarReceita([FromBody] ReceitaRequest request)
        {
            var receita = await _geminiServices.GerarReceita(request.Ingredientes);
            if (receita == null)
                return StatusCode(500, "Não consegui gerar receita.");
            return Ok(receita);
        }
    }
}