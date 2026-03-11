using AllineamentoAnagrafiche.Data;
using AllineamentoAnagrafiche.DTOs;
using AllineamentoAnagrafiche.Models;
using AllineamentoAnagrafiche.Models.ViewModels;
using AllineamentoAnagrafiche.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Security.Claims;
using static Microsoft.CodeAnalysis.CSharp.SyntaxTokenParser;

namespace AllineamentoAnagrafiche.Controllers
{
    public class UtentiController : BaseController
    {
        private readonly AutorizzazioniService autorizzazioniService;
        public UtentiController(AnagraficheContext context, AuthService auth, LogService log, AutorizzazioniService autorizzazioni)
            :base(context, auth, log)
        {
            autorizzazioniService = autorizzazioni;
        }

        [AllowAnonymous]
        public IActionResult Registra()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> RegistraUtente([FromBody]UserDto user)
        {
            string metodo = Costanti.RegistraUtente;
            string request = $"{{ \"Username\": \"{user.Username}\", \"Password\": \"********\" }}";

            if (string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password))
            {
                _logService.RegistraLog(metodo, request, "AE: Dati non inseriti correttamente");
                _dbContext.SaveChanges();
                return BadRequest("Dati non inseriti correttamente");
            }

            bool userEsistente = _dbContext.TUtentis.Any(u => u.Username.Equals(user.Username));
            if (userEsistente)
            {
                _logService.RegistraLog(metodo, request, "AE: Username già esistente");
                _dbContext.SaveChanges();
                return BadRequest("Lo username scelto non è disponibile");
            }

            using var hmac = new System.Security.Cryptography.HMACSHA512();

            TUtenti nuovoUtente = new()
            {
                Username = user.Username,

                Salt = hmac.Key,

                PasswordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(user.Password)),
            };

            _dbContext.TUtentis.Add(nuovoUtente);
            _dbContext.SaveChanges();

            autorizzazioniService.AssegnaPermessiBase(nuovoUtente.UserCodice);

            await CreaCookie(nuovoUtente);

            _logService.RegistraLog(metodo, request, "AA");
            _dbContext.SaveChanges();

            return Ok();
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> LoginUtente([FromBody]UserDto user)
        {
            string metodo = Costanti.LoginUtente;
            string request = $"{{ \"Username\": \"{user.Username}\", \"Password\": \"********\" }}";

            TUtenti? utenteDb = _dbContext.TUtentis.FirstOrDefault(u => u.Username == user.Username);

            if(utenteDb == null)
            {
                _logService.RegistraLog(metodo, request, "AE: Credenziali non inserite correttamente");
                _dbContext.SaveChanges();
                return BadRequest("Credenziali non inserite correttamente");
            }

            using var hmac = new System.Security.Cryptography.HMACSHA512(utenteDb.Salt);

            var hashCalcolato = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(user.Password));

            if (hashCalcolato.SequenceEqual(utenteDb.PasswordHash)){

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, utenteDb.Username),
                    new Claim("UserCodice", utenteDb.UserCodice.ToString())
                };

                var autorizzazioniUtente = _dbContext.TAutorizzazionis
                    .Where(a => a.UserCodice == utenteDb.UserCodice)
                    .Select(a => a.MetodoCodiceNavigation.NomeMetodo)
                    .ToList();

                foreach (var nomePermesso in autorizzazioniUtente)
                {
                    claims.Add(new Claim("Permission", nomePermesso));
                }

                var claimsIdentity = new ClaimsIdentity(claims, "CookieAuthScheme");

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = false
                };

                await HttpContext.SignInAsync("CookieAuthScheme", new ClaimsPrincipal(claimsIdentity), authProperties);
                _logService.RegistraLog(metodo, request, "AA", utenteDb.UserCodice);
                _dbContext.SaveChanges();
                return Ok();
            }
            _logService.RegistraLog(metodo, request, "AE: Credenziali non inserite correttamente");
            _dbContext.SaveChanges();
            return BadRequest("Credenziali non inserite correttamente");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuthScheme");
            return RedirectToAction("Index", "Home");
        }

        public async Task CreaCookie(TUtenti nuovoUtente)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, nuovoUtente.Username),
                new("UserCodice", nuovoUtente.UserCodice.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, "CookieAuthScheme");

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = false
            };

            await HttpContext.SignInAsync("CookieAuthScheme", new ClaimsPrincipal(claimsIdentity), authProperties);
        }    
    }
}
