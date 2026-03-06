using System;
using System.Collections.Generic;

namespace AllineamentoAnagrafiche.Models;

public partial class Autorizzazione
{
    public long AutorizzazioneCodice { get; set; }

    public long UserCodice { get; set; }

    public string? NomeMetodo { get; set; }

    public virtual Utente UserCodiceNavigation { get; set; } = null!;
}
