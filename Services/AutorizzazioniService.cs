using AllineamentoAnagrafiche.Data;
using AllineamentoAnagrafiche.Models;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
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
            var autorizzazioni = new List<string> {
                Costanti.RegioniVisualizza,
                Costanti.ProvinceVisualizza,
                Costanti.ComuniVisualizza
            };

            foreach (var autorizzazione in autorizzazioni)
            {
                TTipoAutorizzazioni? tipoAutorizzazione = _dbContext.TTipoAutorizzazionis.FirstOrDefault(t => t.NomeMetodo.Equals(autorizzazione));
                InserisciAutorizzazione(new TAutorizzazioni
                {
                    UserCodice = codiceUtente,
                    MetodoCodice = tipoAutorizzazione.TipoAutorizzazioneCodice
                });
            }
        }

        public String InserisciAutorizzazione(TAutorizzazioni auth)
        {
            bool giaPresente = _dbContext.TAutorizzazionis.Any(a =>
                a.UserCodice == auth.UserCodice &&
                a.MetodoCodice == auth.MetodoCodice);

            if (!giaPresente)
            {
                TAutorizzazioni newAuth = new()
                {
                    UserCodice = auth.UserCodice,
                    MetodoCodice = auth.MetodoCodice
                };
                _dbContext.TAutorizzazionis.Add(newAuth);
                return "AA";
            }

            return "AE: Autorizzazione già presente";
        }

        public String EliminaAutorizzazione(long idAutorizzazione)
        {
            TAutorizzazioni? auth = _dbContext.TAutorizzazionis.Find(idAutorizzazione);

            if (auth == null)
            {
                return "AE: elemento non presente nel DB";
            }

            _dbContext.TAutorizzazionis.Remove(auth);
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
