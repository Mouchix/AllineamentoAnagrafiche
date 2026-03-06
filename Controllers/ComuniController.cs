using AllineamentoAnagrafiche.Data;
using AllineamentoAnagrafiche.DTOs;
using AllineamentoAnagrafiche.Models;
using AllineamentoAnagrafiche.Models.ViewModels;
using AllineamentoAnagrafiche.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml;

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

        public IActionResult EsportaComuni(int? codiceProvincia, int? codiceRegione)
        {
            if (!CheckPermission(Costanti.ComuniVisualizza)) return Forbid();

            var query = from comune in _dbContext.Comuni
                        join provincia in _dbContext.Province
                        on comune.ComProCodice equals provincia.ProCodice
                        join regione in _dbContext.Regioni
                        on provincia.ProRegCodice equals regione.RegCodice
                        select new { comune, provincia, regione };

            if (codiceProvincia.HasValue && codiceProvincia > 0)
            {
                query = query.Where(x => x.provincia.ProCodice == codiceProvincia);
            }

            if (codiceRegione.HasValue && codiceRegione > 0)
            {
                query = query.Where(x => x.regione.RegCodice == codiceRegione);
            }

            var listaComuni = query.ToList();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Comuni");

            string[] headers = { "Codice ISTAT Comune", "Nome Comune", "Data Inizio Validità", "Data Fine Validità", "Codice ISTAT Provincia", "Nome Provincia", "Codice ISTAT Regione", "Nome Regione" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cells[1, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
            }

            worksheet.Column(3).Style.Numberformat.Format = "dd/mm/yyyy";
            worksheet.Column(4).Style.Numberformat.Format = "dd/mm/yyyy";

            for (int i = 0; i < listaComuni.Count; i++)
            {
                worksheet.Cells[i + 2, 1].Value = listaComuni[i].comune.ComIstat;
                worksheet.Cells[i + 2, 2].Value = listaComuni[i].comune.ComDescrizione;
                worksheet.Cells[i + 2, 3].Value = listaComuni[i].comune.ComInizioValidita.ToString("dd/MM/yyyy");
                worksheet.Cells[i + 2, 4].Value = listaComuni[i].comune.ComFineValidita.ToString("dd/MM/yyyy");
                worksheet.Cells[i + 2, 5].Value = listaComuni[i].provincia.ProIstat;
                worksheet.Cells[i + 2, 6].Value = listaComuni[i].provincia.ProDescrizione;
                worksheet.Cells[i + 2, 7].Value = listaComuni[i].regione.RegIstat;
                worksheet.Cells[i + 2, 8].Value = listaComuni[i].regione.RegDescrizione;
            }

            worksheet.Cells.AutoFitColumns();

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            string fileName = $"Comuni.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
