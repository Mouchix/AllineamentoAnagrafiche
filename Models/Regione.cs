using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AllineamentoAnagrafiche.Models;

public partial class Regione
{
    public int RegCodice { get; set; }
    public string RegIstat { get; set; } = null!;
    public string RegDescrizione { get; set; } = null!;
    public DateTime RegInizioValidita { get; set; }
    public DateTime RegFineValidita { get; set; }
    public virtual ICollection<Provincia> TProvinces { get; set; } = new List<Provincia>();
}
