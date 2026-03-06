using AllineamentoAnagrafiche.Data;
using AllineamentoAnagrafiche.DTOs;
using AllineamentoAnagrafiche.Models;
using AllineamentoAnagrafiche.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using System.Diagnostics.Eventing.Reader;
using System.Security.Claims;

[Authorize]
public abstract class BaseController : Controller
{
    protected readonly AuthService _authService;
    protected readonly LogService _logService;
    protected readonly AnagraficheContext _dbContext;

    public BaseController(AnagraficheContext db, AuthService auth, LogService log)
    {
        _dbContext = db;
        _authService = auth;
        _logService = log;
    }

    protected bool CheckPermission(string metodo)
    {
        var claim = User.FindFirst("UserCodice")?.Value;
        long.TryParse(claim, out long codiceUtente);
        return _authService.VerificaAutorizzazione(codiceUtente, metodo);
    }

    protected AuthResponse CheckUser(String metodo)
    {
        Utente? utenteDb = null;

        if (User.Identity?.IsAuthenticated == true)
        {
            utenteDb = _authService.VerificaAutenticazioneCookie(User);
        }

        if (utenteDb == null)
        {
            string? headerAuth = Request.Headers["Auth"];
            if (!string.IsNullOrEmpty(headerAuth))
            {
                utenteDb = _authService.VerificaAutenticazioneHeader(headerAuth);
            }
        }

        if (utenteDb == null)
        {
            return new AuthResponse { Response = "AE: Credenziali non inserite correttamente" };
        }

        if (!_authService.VerificaAutorizzazione(utenteDb.UserCodice, metodo))
        {
            return new AuthResponse { Response = "AE: Utente non autorizzato", Utente = utenteDb };
        }

        return new AuthResponse { Response = "AA", Utente = utenteDb };
    }
}