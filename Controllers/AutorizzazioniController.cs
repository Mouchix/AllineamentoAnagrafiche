using AllineamentoAnagrafiche.Data;
using AllineamentoAnagrafiche.DTOs;
using AllineamentoAnagrafiche.Models;
using AllineamentoAnagrafiche.Models.ViewModels;
using AllineamentoAnagrafiche.Services;
using Humanizer.Localisation;
using Microsoft.AspNetCore.Mvc;

namespace AllineamentoAnagrafiche.Controllers
{
    public class AutorizzazioniController : BaseController
    {
        private readonly AutorizzazioniService autorizzazioniService;
        public AutorizzazioniController(AnagraficheContext context, AuthService auth, LogService log, AutorizzazioniService autorizzazioni)
            : base(context, auth, log)
        {
            autorizzazioniService = autorizzazioni;
        }

        public IActionResult GestisciAutorizzazioni()
        {
            if (!CheckPermission(Costanti.VisualizzaAutorizzazioni)) return Forbid();

            ViewBag.TipiAutorizzazioni = autorizzazioniService.GetTipiAutorizzazioni();

            return View();
        }

        public IActionResult CreaAutorizzazione()
        {
            if (!CheckPermission(Costanti.VisualizzaAutorizzazioni)) return Forbid();

            ViewBag.TipiAutorizzazioni = autorizzazioniService.GetTipiAutorizzazioni();

            return View();
        }

        public IActionResult EliminaAutorizzazione(int? codiceAutorizzazione)
        {
            if (!CheckPermission(Costanti.VisualizzaAutorizzazioni)) return Forbid();

            AutorizzazioneVM? autorizzazione = GetAutorizzazione(codiceAutorizzazione);
            return View(autorizzazione);
        }

        [HttpPost]
        public IActionResult InserisciAutorizzazione([FromBody] NewAuthorizationRequest authorization)
        {
            string metodo = Costanti.NuovaAutorizzazione;
            string request = System.Text.Json.JsonSerializer.Serialize(authorization);

            AuthResponse result = CheckUser(Costanti.VisualizzaAutorizzazioni);

            if (result.Response.Equals("AA"))
            {
                var utenteTarget = _dbContext.Utenti.FirstOrDefault(u => u.Username == authorization.Username);
                if (utenteTarget == null)
                {
                    result.Response = "AE: Username non valido";
                }
                else
                {
                    result.Response = autorizzazioniService.InserisciAutorizzazione(new Autorizzazione
                    {
                        UserCodice = utenteTarget.UserCodice,
                        NomeMetodo = authorization.NomeMetodo
                    });
                }
            }
            _logService.RegistraLog(metodo, request, result.Response, result.Utente?.UserCodice ?? Costanti.SystemUserId);
            _dbContext.SaveChanges();
            return result.Response == "AA" ? Ok() : BadRequest(result.Response);
        }

        [HttpPost]
        public IActionResult EliminaAutorizzazione(int codiceAutorizzazione)
        {
            string metodo = Costanti.EliminaAutorizzazione;

            AuthResponse result = CheckUser(Costanti.VisualizzaAutorizzazioni);

            if (result.Response.Equals("AA"))
            {
                result.Response = autorizzazioniService.EliminaAutorizzazione(codiceAutorizzazione);
            }

            _logService.RegistraLog(metodo, codiceAutorizzazione.ToString(), result.Response, result.Utente?.UserCodice ?? Costanti.SystemUserId);
            _dbContext.SaveChanges();
            return result.Response.Equals("AA") ? Ok() : BadRequest(result.Response);
        }

        [HttpGet]
        public IActionResult GetAutorizzazioni(string searchterm, string autorizzazioneFiltro)
        {
            if (!CheckPermission(Costanti.VisualizzaAutorizzazioni)) return Forbid();

            var query = from utente in _dbContext.Utenti
                        join autorizzazione in _dbContext.Autorizzazioni
                        on utente.UserCodice equals autorizzazione.UserCodice
                        select new { utente, autorizzazione };

            if (!string.IsNullOrEmpty(searchterm))
            {
                query = query.Where(x => x.utente.Username.Contains(searchterm));
            }

            if (!string.IsNullOrEmpty(autorizzazioneFiltro))
            {
                query = query.Where(x => x.autorizzazione.NomeMetodo == autorizzazioneFiltro);
            }

            var result = query.Select(x => new AutorizzazioneVM
            {
                CodiceAutorizzazione = x.autorizzazione.AutorizzazioneCodice,
                Username = x.utente.Username,
                NomeMetodo = x.autorizzazione.NomeMetodo
            }).ToList();

            return Json(result);
        }

        [HttpGet]
        public AutorizzazioneVM? GetAutorizzazione(long? codiceAutorizzazione)
        {
            if (!CheckPermission(Costanti.VisualizzaAutorizzazioni)) return null;

            Autorizzazione? autorizzazioneDb = _dbContext.Autorizzazioni.Find(codiceAutorizzazione);

            if (autorizzazioneDb != null)
            {
                Utente? utenteDb = _dbContext.Utenti.Find(autorizzazioneDb.UserCodice);

                if(utenteDb != null)
                {
                    return new()
                    {
                        CodiceAutorizzazione = autorizzazioneDb.AutorizzazioneCodice,
                        Username = utenteDb.Username,
                        NomeMetodo = autorizzazioneDb.NomeMetodo
                    };
                }

            }
            return null;
        }
        
    }
}
