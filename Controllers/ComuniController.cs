using AllineamentoAnagrafiche.Data;
using AllineamentoAnagrafiche.DTOs;
using AllineamentoAnagrafiche.Models;
using AllineamentoAnagrafiche.Models.ViewModels;
using AllineamentoAnagrafiche.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace AllineamentoAnagrafiche.Controllers
{
    public class ComuniController : BaseController
    {
        private readonly UpsertService<Regione, RegioneDto> rUpsertService;
        private readonly UpsertService<Provincia, ProvinciaDto> pUpsertService;
        private readonly UpsertService<Comune, ComuneDto> cUpsertService;
        private readonly RemoveService<Comune, Comune> removeService;

        public ComuniController(AnagraficheContext db, AuthService auth, UpsertService<Regione, RegioneDto> rService, UpsertService<Provincia, ProvinciaDto> pService, UpsertService<Comune, ComuneDto> cService, RemoveService<Comune, Comune> remove, LogService lService)
            : base(db, auth, lService)
        {
            this.rUpsertService = rService;
            this.pUpsertService = pService;
            this.cUpsertService = cService;
            this.removeService = remove;
        }

        public IActionResult IndexComuni()
        {
            if (!CheckPermission(Costanti.ComuniVisualizza)) return Forbid();
            ViewBag.PuoCreare = CheckPermission(Costanti.ComuniUpsert);
            ViewBag.PuoEliminare = CheckPermission(Costanti.ComuniDelete);
            return View();
        }

        public IActionResult CreaComune()
        {
            if (!CheckPermission(Costanti.ComuniVisualizza) || !CheckPermission(Costanti.ComuniUpsert)) return Forbid();
            return View();
        }

        public IActionResult ModificaComune(int? codiceComune, int? codiceProvincia, int? codiceRegione)
        {
            if (!CheckPermission(Costanti.ComuniVisualizza) || !CheckPermission(Costanti.ComuniUpsert)) return Forbid();
            Comune? comuneFromDb = _dbContext.Comuni.Find(codiceComune);
            Provincia? provinciaFromDb = _dbContext.Province.Find(codiceProvincia);
            Regione? regioneFromDb = _dbContext.Regioni.Find(codiceRegione);

            ComuneVM comuneVM = new()
            {
                Comune = comuneFromDb,
                Provincia = provinciaFromDb,
                Regione = regioneFromDb,
            };
            return View(comuneVM);
        }

        public IActionResult EliminaComune(int? id)
        {
            if (!CheckPermission(Costanti.ComuniVisualizza) || !CheckPermission(Costanti.ComuniDelete)) return Forbid();

            var comuneFromDb = _dbContext.Comuni.Find(id);

            return View(comuneFromDb);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult AggiornaComuni([FromBody]ComuneRootDto comuni)
        {
            string metodo = Costanti.ComuniUpsert;
            string request = System.Text.Json.JsonSerializer.Serialize(comuni);

            AuthResponse result = CheckUser(metodo);

            if (result.Response.Equals("AA"))
            {
                using var transaction = _dbContext.Database.BeginTransaction();

                try
                {
                    if (!comuni.Comuni.IsNullOrEmpty())
                    {
                        foreach (ComuneDto comune in comuni.Comuni)
                        {
                            int idRegioneReferenziata = this.rUpsertService.Upsert(comune.Provincia.Regione, r => r.RegIstat == comune.Provincia.Regione.CodiceISTAT);

                            int idProvinciaReferenziata = this.pUpsertService.Upsert(comune.Provincia, p => p.ProIstat == comune.Provincia.CodiceISTAT, p => p.ProRegCodice = idRegioneReferenziata);

                            this.cUpsertService.Upsert(comune, c => c.ComIstat == comune.CodiceISTAT, c => c.ComProCodice = idProvinciaReferenziata);
                        }
                        result.Response = "AA";
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
        public IActionResult CancellaComuni([FromBody]DeleteByIstatRequest request)
        {
            string metodo = Costanti.ComuniDelete;

            AuthResponse result = CheckUser(metodo);

            if (result.Response.Equals("AA"))
            {
                result.Response = removeService.Remove(request.ForzaCancellazione, c => c.ComIstat == request.CodiceISTAT);
            }

            _logService.RegistraLog(metodo, request.CodiceISTAT, result.Response + " - Eliminazione forzata: " + request.ForzaCancellazione, result.Utente?.UserCodice ?? Costanti.SystemUserId);

            this._dbContext.SaveChanges();
            return result.Response.Equals("AA") ? Ok() : BadRequest(result.Response);
        }

        [HttpGet]
        public IActionResult GetComuni(string searchTerm = "", int? regioneFiltro = null, int? provinciaFiltro = null)
        {
            if (!CheckPermission(Costanti.ComuniVisualizza)) return Forbid();
            var comuni = _dbContext.Comuni.AsQueryable();

            if (regioneFiltro != null)
            {
                comuni = comuni.Where(c => c.ComProCodiceNavigation.ProRegCodice == regioneFiltro);
            }

            if (provinciaFiltro != null)
            {
                comuni = comuni.Where(c => c.ComProCodice == provinciaFiltro);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                comuni = comuni.Where(c => c.ComDescrizione.Contains(searchTerm));
            }

            var result = comuni.Select(c => new {
                codice = c.Codice,
                descrizione = c.Descrizione,
                istat = c.Istat,
                inizioValidita = c.InizioValidita,
                fineValidita = c.FineValidita,
                nomeProvincia = c.ComProCodiceNavigation.Descrizione,
                codiceProvincia = c.ComProCodiceNavigation.Codice,
                codiceRegione = c.ComProCodiceNavigation.ProRegCodiceNavigation.Codice
            }).ToList();

            return Json(result.Take(75));
        }
    }
}
