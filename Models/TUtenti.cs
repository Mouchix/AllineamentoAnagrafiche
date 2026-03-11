namespace AllineamentoAnagrafiche.Models;

public partial class TUtenti
{
    public long UserCodice { get; set; }

    public string Username { get; set; } = null!;

    public byte[] Salt { get; set; } = null!;

    public byte[] PasswordHash { get; set; } = null!;

    public virtual ICollection<TAutorizzazioni> TAutorizzazionis { get; set; } = new List<TAutorizzazioni>();

    public virtual ICollection<TLogMessaggi> TLogMessaggis { get; set; } = new List<TLogMessaggi>();
}
