using AllineamentoAnagrafiche.Data;
using AllineamentoAnagrafiche.DTOs;
using AllineamentoAnagrafiche.Models;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Core.Types;
using System.Security.Claims;

namespace AllineamentoAnagrafiche.Services
{
    public class AuthService
    {
        private readonly AnagraficheContext _dbContext;
        private readonly LogService logService;

        public AuthService(AnagraficheContext context, LogService logService)
        {
            this._dbContext = context;
            this.logService = logService;
        }

        public Utente? VerificaAutenticazioneHeader(string loginHeader)
        {
            if (string.IsNullOrEmpty(loginHeader)) return null;

            var parts = loginHeader.Split(':', 2);
            if (parts.Length != 2) return null;

            string username = parts[0];
            string password = parts[1];

            var utente = _dbContext.Utenti.FirstOrDefault(u => u.Username == username);
            if (utente == null) return null;

            using var hmac = new System.Security.Cryptography.HMACSHA512(utente.Salt);
            var hashCalcolato = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));

            return hashCalcolato.SequenceEqual(utente.PasswordHash) ? utente : null;
        }

        public Utente? VerificaAutenticazioneCookie(ClaimsPrincipal user)
        {
            if (user.Identity == null || !user.Identity.IsAuthenticated) return null;
 
            var claimCodice = user.FindFirst("UserCodice")?.Value;

            if (string.IsNullOrEmpty(claimCodice) || !long.TryParse(claimCodice, out long codice))
                return null;

            return _dbContext.Utenti.FirstOrDefault(u => u.UserCodice == codice);
        }

        public bool VerificaAutorizzazione(long userCodice, string metodo)
        {
            return _dbContext.Autorizzazioni.Any(a =>
                a.UserCodice == userCodice && a.NomeMetodo == metodo);
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
    }
}
