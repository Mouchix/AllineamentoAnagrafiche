using System;
using System.Collections.Generic;

namespace AllineamentoAnagrafiche.Models;

public partial class MessageLog
{
    public long LogCodice { get; set; }
    public long LogUser { get; set; }
    public string LogMetodo { get; set; } = null!;

    public DateTime LogDataOra { get; set; }

    public string LogMessaggioRequest { get; set; } = null!;

    public string LogMessaggioResponse { get; set; } = null!;
    public virtual Utente LogUserNavigation { get; set; } = null!;
}
