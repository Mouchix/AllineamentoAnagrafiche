using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AllineamentoAnagrafiche.Models;

public partial class Provincia
{
    public int ProCodice { get; set; }
    public string ProIstat { get; set; } = null!;
    public string ProDescrizione { get; set; } = null!;
    public DateTime ProInizioValidita { get; set; }
    public DateTime ProFineValidita { get; set; }
    public int ProRegCodice { get; set; }
    public virtual Regione? ProRegCodiceNavigation { get; set; } = null!;
    public virtual ICollection<Comune> TComunis { get; set; } = new List<Comune>();
}
