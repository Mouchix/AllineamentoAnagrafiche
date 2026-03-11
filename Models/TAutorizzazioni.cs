namespace AllineamentoAnagrafiche.Models;

public partial class TAutorizzazioni
{
    public long AutorizzazioneCodice { get; set; }

    public long UserCodice { get; set; }

    public long? MetodoCodice { get; set; }

    public virtual TTipoAutorizzazioni? MetodoCodiceNavigation { get; set; }

    public virtual TUtenti UserCodiceNavigation { get; set; } = null!;
}
