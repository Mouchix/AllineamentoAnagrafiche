using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AllineamentoAnagrafiche.Models;

public partial class Comune
{
    public int ComCodice { get; set; }
    public string ComIstat { get; set; } = null!;
    public string ComDescrizione { get; set; } = null!;
    public DateTime ComInizioValidita { get; set; }
    public DateTime ComFineValidita { get; set; }
    public int ComProCodice { get; set; }
    public virtual Provincia? ComProCodiceNavigation { get; set; } = null!;
}
