using AllineamentoAnagrafiche.Data;
using AllineamentoAnagrafiche.DTOs;
using AllineamentoAnagrafiche.Models;
using AllineamentoAnagrafiche.Models.ViewModels;
using AllineamentoAnagrafiche.Services;
using Humanizer.Localisation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using System.Net;

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
            if (!User.HasClaim("Permission", Costanti.VisualizzaAutorizzazioni)) return Forbid();

            ViewBag.TipiAutorizzazioni = autorizzazioniService.GetTipiAutorizzazioni();

            return View();
        }

        public IActionResult CreaAutorizzazione()
        {
            if (!User.HasClaim("Permission", Costanti.VisualizzaAutorizzazioni)) return Forbid();

            ViewBag.TipiAutorizzazioni = autorizzazioniService.GetTipiAutorizzazioni();

            return View();
        }

        public IActionResult EliminaAutorizzazione(int? codiceAutorizzazione)
        {
            if (!User.HasClaim("Permission", Costanti.VisualizzaAutorizzazioni)) return Forbid();

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
                var utenteTarget = _dbContext.TUtentis.FirstOrDefault(u => u.Username == authorization.Username);
                if (utenteTarget == null)
                {
                    result.Response = "AE: Username non valido";
                }
                else
                {
                    TTipoAutorizzazioni? tipoAutorizzazione = GetTipoAutorizzazione(authorization.NomeMetodo);
                    if(tipoAutorizzazione != null)
                    {
                        result.Response = autorizzazioniService.InserisciAutorizzazione(new TAutorizzazioni
                        {
                            UserCodice = utenteTarget.UserCodice,
                            MetodoCodice = tipoAutorizzazione.TipoAutorizzazioneCodice
                        });
                    }
                    else
                    {
                        result.Response = "AE: Tipo Autorizzazione non valido";
                    }
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
            if (!User.HasClaim("Permission", Costanti.VisualizzaAutorizzazioni)) return Forbid();

            var query = from autorizzazione in _dbContext.TAutorizzazionis
                        join utente in _dbContext.TUtentis
                        on autorizzazione.UserCodice equals utente.UserCodice
                        join tipoAutorizzazione in _dbContext.TTipoAutorizzazionis
                        on autorizzazione.MetodoCodice equals tipoAutorizzazione.TipoAutorizzazioneCodice
                        select new { utente, autorizzazione, tipoAutorizzazione };

            if (!string.IsNullOrEmpty(searchterm))
            {
                query = query.Where(x => x.utente.Username.Contains(searchterm));
            }

            if (!string.IsNullOrEmpty(autorizzazioneFiltro))
            {
                query = query.Where(x => x.tipoAutorizzazione.NomeMetodo == autorizzazioneFiltro);
            }              

            var result = query.Select(x => new AutorizzazioneVM
            {
                CodiceAutorizzazione = x.autorizzazione.AutorizzazioneCodice,
                Username = x.utente.Username,
                NomeMetodo = x.tipoAutorizzazione.NomeMetodo
            }).ToList();

            return Json(result);
        }

        [HttpGet]
        public AutorizzazioneVM? GetAutorizzazione(long? codiceAutorizzazione)
        {
            if (!User.HasClaim("Permission", Costanti.VisualizzaAutorizzazioni)) return null;

            TAutorizzazioni? autorizzazioneDb = _dbContext.TAutorizzazionis.Find(codiceAutorizzazione);

            if (autorizzazioneDb != null)
            {
                TUtenti? utenteDb = _dbContext.TUtentis.Find(autorizzazioneDb.UserCodice);
                TTipoAutorizzazioni? tipoAutorizzazione = _dbContext.TTipoAutorizzazionis.Find(autorizzazioneDb.MetodoCodice);

                if(utenteDb != null && tipoAutorizzazione !=  null)
                {
                    return new()
                    {
                        CodiceAutorizzazione = autorizzazioneDb.AutorizzazioneCodice,
                        Username = utenteDb.Username,
                        NomeMetodo = tipoAutorizzazione.NomeMetodo
                    };
                }

            }
            return null;
        }
        
        private TTipoAutorizzazioni? GetTipoAutorizzazione(string nomeMetodo)
        {
            return _dbContext.TTipoAutorizzazionis.FirstOrDefault(t => t.NomeMetodo.Equals(nomeMetodo));
        }
    }
}
