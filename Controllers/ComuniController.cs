using AllineamentoAnagrafiche.Data;
using AllineamentoAnagrafiche.DTOs;
using AllineamentoAnagrafiche.Models;
using AllineamentoAnagrafiche.Models.ViewModels;
using AllineamentoAnagrafiche.Services;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml;
using System.Globalization;
using System.Text;

namespace AllineamentoAnagrafiche.Controllers
{
    public class ComuniController : BaseController
    {
        private readonly UpsertService<TRegioni, RegioneDto> rUpsertService;
        private readonly UpsertService<TProvince, ProvinciaDto> pUpsertService;
        private readonly UpsertService<TComuni, ComuneDto> cUpsertService;
        private readonly RemoveService<TComuni, TComuni> removeService;

        public ComuniController(AnagraficheContext db, AuthService auth, UpsertService<TRegioni, RegioneDto> rService, UpsertService<TProvince, ProvinciaDto> pService, UpsertService<TComuni, ComuneDto> cService, RemoveService<TComuni, TComuni> remove, LogService lService)
            : base(db, auth, lService)
        {
            this.rUpsertService = rService;
            this.pUpsertService = pService;
            this.cUpsertService = cService;
            this.removeService = remove;
        }

        public IActionResult IndexComuni()
        {
            if (!User.HasClaim("Permission", Costanti.ComuniVisualizza)) return Forbid();
            ViewBag.PuoCreare = User.HasClaim("Permission", Costanti.ComuniUpsert);
            ViewBag.PuoEliminare = User.HasClaim("Permission", Costanti.ComuniDelete);
            return View();
        }

        public IActionResult CreaComune()
        {
            if (!User.HasClaim("Permission", Costanti.ComuniVisualizza) || !User.HasClaim("Permission", Costanti.ComuniUpsert)) return Forbid();
            return View();
        }

        public IActionResult ModificaComune(int? codiceComune, int? codiceProvincia, int? codiceRegione)
        {
            if (!User.HasClaim("Permission", Costanti.ComuniVisualizza) || !User.HasClaim("Permission", Costanti.ComuniUpsert)) return Forbid();
            TComuni? comuneFromDb = _dbContext.TComunis.Find(codiceComune);
            TProvince? provinciaFromDb = _dbContext.TProvinces.Find(codiceProvincia);
            TRegioni? regioneFromDb = _dbContext.TRegionis.Find(codiceRegione);

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
            if (!User.HasClaim("Permission", Costanti.ComuniVisualizza) || !User.HasClaim("Permission", Costanti.ComuniDelete)) return Forbid();

            var comuneFromDb = _dbContext.TComunis.Find(id);

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
            if (!User.HasClaim("Permission", Costanti.ComuniVisualizza)) return Forbid();
            var comuni = _dbContext.TComunis.AsQueryable();

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

            return Json(result.Take(50));
        }

        public IActionResult EsportaComuni(string tipo, int? codiceProvincia, int? codiceRegione)
        {
            if (!User.HasClaim("Permission", Costanti.ComuniVisualizza)) return Forbid();

            var query = from comune in _dbContext.TComunis
                        join provincia in _dbContext.TProvinces
                        on comune.ComProCodice equals provincia.ProCodice
                        join regione in _dbContext.TRegionis
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

            var dati = query.Select(d => new {
                Codice_Istat_Comune = d.comune.ComIstat,
                Nome_Comune = d.comune.ComDescrizione,
                Data_Inizio_Validita = d.comune.ComInizioValidita.ToString("dd/MM/yyyy"),
                Data_Fine_Validita = d.comune.ComFineValidita.ToString("dd/MM/yyyy"),
                Codice_Istat_Provincia = d.provincia.ProIstat,
                Nome_Provincia = d.provincia.ProDescrizione,
                Codice_Istat_Regione = d.regione.RegIstat,
                Nome_Regione = d.regione.RegDescrizione,
            }).ToList();

            if ("xlsx".Equals(tipo))
            {
                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("Comuni");

                ws.Cells["A1"].LoadFromCollection(dati, true);

                string[] customHeaders = { "Codice ISTAT Comune", "Nome Comune", " Data Inizio Validità", "Data Fine Validità", "Codice ISTAT Provincia", "Nome Provincia", "Codice ISTAT Regione", "Nome Regione" };
                for (int i = 0; i < customHeaders.Length; i++)
                {
                    ws.Cells[1, i + 1].Value = customHeaders[i];
                }

                using (var headerRange = ws.Cells[1, 1, 1, 8])
                {
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                ws.Cells.AutoFitColumns();

                return File(package.GetAsByteArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "comuni.xlsx");
            }

            if ("csv".Equals(tipo, StringComparison.OrdinalIgnoreCase))
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = ";" };

                using var ms = new MemoryStream();
                using var sw = new StreamWriter(ms, Encoding.UTF8);
                using var csv = new CsvWriter(sw, config);
                csv.WriteRecords(dati);
                sw.Flush();
                return File(ms.ToArray(), "text/csv", "comuni.csv");
            }

            return BadRequest("Formato non supportato");
        }
    }
}
