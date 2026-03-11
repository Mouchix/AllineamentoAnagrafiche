using AllineamentoAnagrafiche.Models;
using System.Text.Json.Serialization;

namespace AllineamentoAnagrafiche.DTOs
{
    public class RegioneRootDto
    {
        [JsonPropertyName("regioni")]
        public List<RegioneDto> Regioni { get; set; } = [];
    }

    public class ProvinciaRootDto
    {
        [JsonPropertyName("province")]
        public List<ProvinciaDto> Province { get; set; } = [];
    }

    public class ComuneRootDto
    {
        [JsonPropertyName("comuni")]
        public List<ComuneDto> Comuni { get; set; } = [];
    }
    public class DeleteByIstatRequest
    {
        [JsonPropertyName("codiceISTAT")]
        public string CodiceISTAT { get; set; } = "";

        [JsonPropertyName("forzaCancellazione")]
        public bool ForzaCancellazione { get; set; } = false;
    }

    public class NewAuthorizationRequest
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = "";

        [JsonPropertyName("nomeMetodo")]
        public string NomeMetodo { get; set; } = "";
    }

    public class AuthResponse
    {
        public TUtenti? Utente { get; set; } = null;
        public String Response { get; set; } = "";
    }
}
