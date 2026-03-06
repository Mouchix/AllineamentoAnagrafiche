using AllineamentoAnagrafiche.Data;
using AllineamentoAnagrafiche.DTOs;
using AllineamentoAnagrafiche.Models;
using AllineamentoAnagrafiche.Models.ViewModels;
using AllineamentoAnagrafiche.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Net;

namespace AllineamentoAnagrafiche.Controllers
{
    public class AutorizzazioniController : BaseController
    {
        public AutorizzazioniController(AnagraficheContext context, AuthService auth, LogService log)
            : base(context, auth, log)
        {
        }
        public IActionResult GestisciAutorizzazioni()
        {
            if (!CheckPermission(Costanti.VisualizzaAutorizzazioni)) return Forbid();
            return View();
        }

        public IActionResult CreaAutorizzazione()
        {
            if (!CheckPermission(Costanti.VisualizzaAutorizzazioni)) return Forbid();
            return View();
        }

        public IActionResult EliminaAutorizzazione(int? codiceAutorizzazione)
        {
            if (!CheckPermission(Costanti.VisualizzaAutorizzazioni)) return Forbid();

            Autorizzazione autorizzazioneDb = GetAutorizzazione(codiceAutorizzazione);
            return View(autorizzazioneDb);
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
                    result.Response = "AE: Dati non inseriti correttamente";
                }
                else
                {
                    result.Response = _authService.InserisciAutorizzazione(new Autorizzazione
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
        public IActionResult EliminaAutorizzazione(long codiceAutorizzazione)
        {
            string metodo = Costanti.EliminaAutorizzazione;

            AuthResponse result = CheckUser(Costanti.VisualizzaAutorizzazioni);

            if (result.Response.Equals("AA"))
            {
                result.Response = _authService.EliminaAutorizzazione(codiceAutorizzazione);
            }

            _logService.RegistraLog(metodo, codiceAutorizzazione.ToString(), result.Response, result.Utente?.UserCodice ?? Costanti.SystemUserId);
            _dbContext.SaveChanges();
            return result.Response.Equals("AA") ? Ok() : BadRequest(result.Response);
        }

        [HttpGet]
        public IActionResult GetAutorizzazioni(string searchterm)
        {
            if (!CheckPermission(Costanti.VisualizzaAutorizzazioni)) return Forbid();

            if (searchterm.IsNullOrEmpty()) return Json(new List<AutorizzazioneVM>());

            var result = (from utente in _dbContext.Utenti
                          join autorizzazione in _dbContext.Autorizzazioni
                          on utente.UserCodice equals autorizzazione.UserCodice
                          where utente.Username.Contains(searchterm)
                          select new AutorizzazioneVM
                          {
                              CodiceAutorizzazione = autorizzazione.AutorizzazioneCodice,
                              Username = utente.Username,
                              NomeMetodo = autorizzazione.NomeMetodo
                          }).ToList();

            return Json(result);
        }

        [HttpGet]
        public Autorizzazione? GetAutorizzazione(int? codiceAutorizzazione)
        {
            if (!CheckPermission(Costanti.VisualizzaAutorizzazioni)) return null;
            return _dbContext.Autorizzazioni.Find(codiceAutorizzazione);
        }
        
    }
}
