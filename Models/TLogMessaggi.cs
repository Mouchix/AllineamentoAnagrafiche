namespace AllineamentoAnagrafiche.Models;

public partial class TLogMessaggi
{
    public long LogCodice { get; set; }

    public long LogUser { get; set; }

    public long LogMetodo { get; set; }

    public DateTime LogDataOra { get; set; }

    public string LogMessaggioRequest { get; set; } = null!;

    public string LogMessaggioResponse { get; set; } = null!;

    public virtual TTipoAutorizzazioni LogMetodoNavigation { get; set; } = null!;

    public virtual TUtenti LogUserNavigation { get; set; } = null!;
}
