namespace AllineamentoAnagrafiche.Models;

public partial class TProvince
{
    public int ProCodice { get; set; }

    public string ProIstat { get; set; } = null!;

    public string ProDescrizione { get; set; } = null!;

    public DateTime ProInizioValidita { get; set; }

    public DateTime ProFineValidita { get; set; }

    public int ProRegCodice { get; set; }

    public virtual TRegioni ProRegCodiceNavigation { get; set; } = null!;

    public virtual ICollection<TComuni> TComunis { get; set; } = new List<TComuni>();
}
