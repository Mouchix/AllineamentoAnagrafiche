namespace AllineamentoAnagrafiche.Models;

public partial class TComuni
{
    public int ComCodice { get; set; }

    public string ComIstat { get; set; } = null!;

    public string ComDescrizione { get; set; } = null!;

    public DateTime ComInizioValidita { get; set; }

    public DateTime ComFineValidita { get; set; }

    public int ComProCodice { get; set; }

    public virtual TProvince ComProCodiceNavigation { get; set; } = null!;
}
