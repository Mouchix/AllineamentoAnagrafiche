using System;
using System.Collections.Generic;

namespace AllineamentoAnagrafiche.Models;

public partial class TTipoAutorizzazioni
{
    public long TipoAutorizzazioneCodice { get; set; }

    public string NomeMetodo { get; set; } = null!;

    public virtual ICollection<TAutorizzazioni> TAutorizzazionis { get; set; } = new List<TAutorizzazioni>();

    public virtual ICollection<TLogMessaggi> TLogMessaggis { get; set; } = new List<TLogMessaggi>();
}
