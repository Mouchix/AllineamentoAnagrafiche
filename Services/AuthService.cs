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

        public TUtenti? VerificaAutenticazioneHeader(string loginHeader)
        {
            if (string.IsNullOrEmpty(loginHeader)) return null;

            var parts = loginHeader.Split(':', 2);
            if (parts.Length != 2) return null;

            string username = parts[0];
            string password = parts[1];

            var utente = _dbContext.TUtentis.FirstOrDefault(u => u.Username == username);
            if (utente == null) return null;

            using var hmac = new System.Security.Cryptography.HMACSHA512(utente.Salt);
            var hashCalcolato = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));

            return hashCalcolato.SequenceEqual(utente.PasswordHash) ? utente : null;
        }

        public TUtenti? VerificaAutenticazioneCookie(ClaimsPrincipal user)
        {
            if (user.Identity == null || !user.Identity.IsAuthenticated) return null;

            var claimCodice = user.FindFirst("UserCodice")?.Value;

            if (string.IsNullOrEmpty(claimCodice) || !long.TryParse(claimCodice, out long codice))
                return null;

            return _dbContext.TUtentis.FirstOrDefault(u => u.UserCodice == codice);
        }



    }   
}
