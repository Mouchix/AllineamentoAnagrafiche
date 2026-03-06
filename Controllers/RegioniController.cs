using AllineamentoAnagrafiche.Data;
using AllineamentoAnagrafiche.DTOs;
using AllineamentoAnagrafiche.Models;
using AllineamentoAnagrafiche.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace AllineamentoAnagrafiche.Controllers
{
    public class RegioniController : BaseController
    {
        private readonly UpsertService<Regione, RegioneDto> upsertService;
        private readonly RemoveService<Regione, Provincia> removeService;

        public RegioniController(AnagraficheContext db, AuthService auth, UpsertService<Regione, RegioneDto> service, RemoveService<Regione, Provincia> remove, LogService lService)
            :base(db, auth, lService)
        {
            this.upsertService = service;
            this.removeService = remove;
        }

        public IActionResult IndexRegioni()
        {
            if (!CheckPermission(Costanti.RegioniVisualizza)) return Forbid();
            ViewBag.PuoCreare = CheckPermission(Costanti.RegioniUpsert);
            ViewBag.PuoEliminare = CheckPermission(Costanti.RegioniDelete);
            return View();
        }

        public IActionResult CreaRegione()
        {
            if (!CheckPermission(Costanti.RegioniVisualizza) || !CheckPermission(Costanti.RegioniUpsert)) return Forbid();
            return View();
        }

        public IActionResult ModificaRegione(int? codiceRegione)
        {
            if (!CheckPermission(Costanti.RegioniVisualizza) || !CheckPermission(Costanti.RegioniUpsert)) return Forbid();

            var regionFromDb = GetRegione(codiceRegione);

            return View(regionFromDb);
        }

        public IActionResult EliminaRegione(int? id)
        {
            if (!CheckPermission(Costanti.RegioniVisualizza) || !CheckPermission(Costanti.RegioniDelete)) return Forbid();

            var regionFromDb = GetRegione(id);

            return View(regionFromDb);
        }

        //POST
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult AggiornaRegioni([FromBody] RegioneRootDto regioni)
        {
            string metodo = Costanti.RegioniUpsert;
            string request = System.Text.Json.JsonSerializer.Serialize(regioni);

            AuthResponse result = CheckUser(metodo);

            if (result.Response.Equals("AA"))
            {
                using var transaction = _dbContext.Database.BeginTransaction();

                try
                {
                    if (!regioni.Regioni.IsNullOrEmpty())
                    {
                        foreach (RegioneDto regione in regioni.Regioni)
                        {
                            this.upsertService.Upsert(regione, r => r.RegIstat == regione.CodiceISTAT);
                        }
                    }
                    else
                    {
                        result.Response = "AE: dati inseriti non correttamente";
                    }
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    _dbContext.ChangeTracker.Clear();
                    result.Response = "AE: " + ex.Message;
                }
            }

            _logService.RegistraLog(metodo, request, result.Response, result.Utente?.UserCodice ?? Costanti.SystemUserId);

            _dbContext.SaveChanges();
            return result.Response.Equals("AA") ? Ok() : BadRequest(result.Response);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult CancellaRegioni([FromBody]DeleteByIstatRequest request)
        {
            string metodo = Costanti.RegioniDelete;

            AuthResponse result = CheckUser(metodo);

            if (result.Response.Equals("AA"))
            {
                result.Response = removeService.Remove(request.ForzaCancellazione, r => r.RegIstat == request.CodiceISTAT, reg => (p => p.ProRegCodice == reg.Codice));
            }

            _logService.RegistraLog(metodo, request.CodiceISTAT + " - Eliminazione forzata: " + request.ForzaCancellazione, result.Response, result.Utente?.UserCodice ?? Costanti.SystemUserId);

            _dbContext.SaveChanges();
            return result.Response.Equals("AA") ? Ok() : BadRequest(result.Response);
        }

        [HttpGet]
        public IActionResult GetRegioni(string searchTerm = "")
        {
            if (!CheckPermission(Costanti.RegioniVisualizza)) return Forbid();

            var regioni = _dbContext.Regioni.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                regioni = regioni.Where(r => r.RegDescrizione.Contains(searchTerm));
            }

            var result = regioni.Select(r => new {
                codice = r.Codice,
                descrizione = r.Descrizione,
                istat = r.Istat,
                inizioValidita = r.InizioValidita,
                fineValidita = r.FineValidita,
            }).ToList();

            return Json(result);
        }

        [HttpGet]
        public Regione? GetRegione(int? codiceRegione)
        {
            if (!CheckPermission(Costanti.RegioniVisualizza)) return null;
            return _dbContext.Regioni.Find(codiceRegione);
        }
    }
}
