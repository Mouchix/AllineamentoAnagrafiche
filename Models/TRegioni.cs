namespace AllineamentoAnagrafiche.Models;

public partial class TRegioni
{
    public int RegCodice { get; set; }

    public string RegIstat { get; set; } = null!;

    public string RegDescrizione { get; set; } = null!;

    public DateTime RegInizioValidita { get; set; }

    public DateTime RegFineValidita { get; set; }

    public virtual ICollection<TProvince> TProvinces { get; set; } = new List<TProvince>();
}
