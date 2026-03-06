using AllineamentoAnagrafiche.Data;
using AllineamentoAnagrafiche.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace AllineamentoAnagrafiche.Services
{
    public class AutorizzazioniService
    {
        private readonly AnagraficheContext _dbContext;
        private readonly LogService logService;

        public AutorizzazioniService(AnagraficheContext context, LogService logService)
        {
            this._dbContext = context;
            this.logService = logService;
        }

        public void AssegnaPermessiBase(long codiceUtente)
        {
            var permessi = new List<string> {
                Costanti.RegioniVisualizza,
                Costanti.ProvinceVisualizza,
                Costanti.ComuniVisualizza
            };

            foreach (var permesso in permessi)
            {
                InserisciAutorizzazione(new Autorizzazione
                {
                    UserCodice = codiceUtente,
                    NomeMetodo = permesso
                });
            }
        }

        public String InserisciAutorizzazione(Autorizzazione auth)
        {
            bool giaPresente = _dbContext.Autorizzazioni.Any(a =>
                a.UserCodice == auth.UserCodice &&
                a.NomeMetodo == auth.NomeMetodo);

            if (!giaPresente)
            {
                Autorizzazione newAuth = new()
                {
                    UserCodice = auth.UserCodice,
                    NomeMetodo = auth.NomeMetodo
                };
                _dbContext.Autorizzazioni.Add(newAuth);
                return "AA";
            }

            return "AE: Autorizzazione già presente";
        }

        public String EliminaAutorizzazione(long idAutorizzazione)
        {
            Autorizzazione? auth = _dbContext.Autorizzazioni.Find(idAutorizzazione);

            if (auth == null)
            {
                return "AE: elemento non presente nel DB";
            }

            _dbContext.Autorizzazioni.Remove(auth);
            _dbContext.SaveChanges();
            return "AA";
        }

        public List<string> GetTipiAutorizzazioni()
        {
            string[] costantiEscluse = { "SystemUserId", "RegistraUtente", "LoginUtente", "NuovaAutorizzazione", "EliminaAutorizzazione" };

            return [.. typeof(Costanti)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && !costantiEscluse.Contains(f.Name))
                .Select(f => f.GetValue(null))
                .Where(v => v != null)
                .Select(v => v!.ToString()!)
                .OrderBy(v => v)];
        }
    }
}
