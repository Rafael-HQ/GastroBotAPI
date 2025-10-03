using Microsoft.AspNetCore.Identity;

namespace GastroBotAPI.Models;

public class User : IdentityUser
{
    public string Documentos { get; set; } = string.Empty;
    
}