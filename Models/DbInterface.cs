using System.ComponentModel.DataAnnotations.Schema;

namespace AllineamentoAnagrafiche.Models
{
    public interface DbInterface
    {
        int Codice { get; }
        string Istat { get; set; }
        string Descrizione { get; set; }
        DateTime InizioValidita { get; set; }
        DateTime FineValidita { get; set; }
    }

    public partial class TRegioni : DbInterface
    {
        [NotMapped]
        public int Codice => this.RegCodice;

        [NotMapped]
        public string Istat { get => this.RegIstat; set => this.RegIstat = value; }

        [NotMapped]
        public string Descrizione { get => this.RegDescrizione; set => this.RegDescrizione = value; }

        [NotMapped]
        public DateTime InizioValidita { get => this.RegInizioValidita; set => this.RegInizioValidita = value; }

        [NotMapped]
        public DateTime FineValidita { get => this.RegFineValidita; set => this.RegFineValidita = value; }
    }

    public partial class TProvince : DbInterface
    {
        [NotMapped]
        public int Codice => this.ProCodice;

        [NotMapped]
        public string Istat { get => this.ProIstat; set => this.ProIstat = value; }

        [NotMapped]
        public string Descrizione { get => this.ProDescrizione; set => this.ProDescrizione = value; }

        [NotMapped]
        public DateTime InizioValidita { get => this.ProInizioValidita; set => this.ProInizioValidita = value; }

        [NotMapped]
        public DateTime FineValidita { get => this.ProFineValidita; set => this.ProFineValidita = value; }
    }

    public partial class TComuni : DbInterface
    {
        [NotMapped]
        public int Codice => this.ComCodice;

        [NotMapped]
        public string Istat { get => this.ComIstat; set => this.ComIstat = value; }

        [NotMapped]
        public string Descrizione { get => this.ComDescrizione; set => this.ComDescrizione = value; }

        [NotMapped]
        public DateTime InizioValidita { get => this.ComInizioValidita; set => this.ComInizioValidita = value; }

        [NotMapped]
        public DateTime FineValidita { get => this.ComFineValidita; set => this.ComFineValidita = value; }
    }
}