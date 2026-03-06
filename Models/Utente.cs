namespace AllineamentoAnagrafiche.Models;

public class Utente
{
    public long UserCodice { get; set; }

    public string Username { get; set; } = null!;

    public byte[] Salt { get; set; } = null!;

    public byte[] PasswordHash { get; set; } = null!;

    public virtual ICollection<Autorizzazione> Autorizzazionis { get; set; } = new List<Autorizzazione>();

    public virtual ICollection<MessageLog> TLogMessaggis { get; set; } = new List<MessageLog>();
}
