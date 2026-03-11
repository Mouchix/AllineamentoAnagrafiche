using AllineamentoAnagrafiche.Data;
using AllineamentoAnagrafiche.DTOs;
using AllineamentoAnagrafiche.Models;
using AllineamentoAnagrafiche.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    protected AuthResponse CheckUser(String metodo)
    {
        TUtenti? utenteDb = null;

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

        if (!User.HasClaim("Permission", metodo))
        {
            return new AuthResponse { Response = "AE: Utente non autorizzato", Utente = utenteDb };
        }

        return new AuthResponse { Response = "AA", Utente = utenteDb };
    }

    [HttpGet]
    public IActionResult VerificaCodiceIstat(string tabella, string istat)
    {
        bool esiste = tabella.ToLower() switch
        {
            "comune" => _dbContext.TComunis.Any(c => c.ComIstat == istat),
            "provincia" => _dbContext.TProvinces.Any(p => p.ProIstat == istat),
            "regione" => _dbContext.TRegionis.Any(r => r.RegIstat == istat),
            _ => false
        };
        return Json(new { esiste });
    }
}