using AllineamentoAnagrafiche.Controllers;

namespace AllineamentoAnagrafiche.Data
{
    public class Costanti
    {
        public const long SystemUserId = 10;

        public const string RegioniVisualizza = "RegioniController.VisualizzaRegioni";
        public const string RegioniUpsert = "RegioniController.AggiornaRegioni";
        public const string RegioniDelete = "RegioniController.CancellaRegioni";

        public const string ProvinceVisualizza = "ProvinceController.VisualizzaProvince";
        public const string ProvinceUpsert = "ProvinceController.AggiornaProvince";
        public const string ProvinceDelete = "ProvinceController.CancellaProvince";

        public const string ComuniVisualizza = "ComuniController.VisualizzaComuni";
        public const string ComuniUpsert = "ComuniController.AggiornaComuni";
        public const string ComuniDelete = "ComuniController.CancellaComuni";

        public const string VisualizzaAutorizzazioni = "AutorizzazioniController.VisualizzaAutorizzazioni";

        //USATE SOLO PER RENDERE PIU PRECISO IL LOG; NON VENGONO UTILIZZATI PER CONTROLLI
        public const string RegistraUtente = "UtentiController.RegistraUtente";
        public const string LoginUtente = "UtentiController.LoginUtente";

        public const string NuovaAutorizzazione = "AutorizzazioniController.InserisciAutorizzazione";
        public const string EliminaAutorizzazione = "AutorizzazioniController.EliminaAutorizzazione";
    }
}
