using System.Text.Json.Serialization;

namespace AllineamentoAnagrafiche.DTOs
{
    public abstract class AnagraficheDto
    {
        [JsonPropertyName("codiceISTAT")]
        public string CodiceISTAT { get; set; } = null!;

        [JsonPropertyName("descrizione")]
        public string Descrizione { get; set; } = null!;

        [JsonPropertyName("inizioValidita")]
        public DateTime InizioValidita { get; set; }

        [JsonPropertyName("fineValidita")]
        public DateTime FineValidita { get; set; }
    }

    public class RegioneDto : AnagraficheDto{}

    public class ProvinciaDto : AnagraficheDto
    {
        [JsonPropertyName("regione")]
        public RegioneDto Regione { get; set; } = null!;
    }

    public class ComuneDto : AnagraficheDto
    {
        [JsonPropertyName("provincia")]
        public ProvinciaDto Provincia { get; set; } = null!;
    }

    public class UserDto
    {
        [JsonPropertyName("username")]
        public required string Username { get; set; }

        [JsonPropertyName("password")]
        public required string Password { get; set; }
    }
}