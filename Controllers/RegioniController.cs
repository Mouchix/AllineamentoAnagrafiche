using AllineamentoAnagrafiche.Data;
using AllineamentoAnagrafiche.DTOs;
using AllineamentoAnagrafiche.Models;
using AllineamentoAnagrafiche.Services;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml;
using System.Globalization;
using System.Text;

namespace AllineamentoAnagrafiche.Controllers
{
    public class RegioniController : BaseController
    {
        private readonly UpsertService<TRegioni, RegioneDto> upsertService;
        private readonly RemoveService<TRegioni, TProvince> removeService;

        public RegioniController(AnagraficheContext db, AuthService auth, UpsertService<TRegioni, RegioneDto> service, RemoveService<TRegioni, TProvince> remove, LogService lService)
            : base(db, auth, lService)
        {
            this.upsertService = service;
            this.removeService = remove;
        }

        public IActionResult IndexRegioni()
        {
            if (!User.HasClaim("Permission", Costanti.RegioniVisualizza)) return Forbid();
            ViewBag.PuoCreare = User.HasClaim("Permission", Costanti.RegioniUpsert);
            ViewBag.PuoEliminare = User.HasClaim("Permission", Costanti.RegioniDelete);
            return View();
        }

        public IActionResult CreaRegione()
        {
            if (!User.HasClaim("Permission", Costanti.RegioniVisualizza) || !User.HasClaim("Permission", Costanti.RegioniUpsert)) return Forbid();
            return View();
        }

        public IActionResult ModificaRegione(int? codiceRegione)
        {
            if (!User.HasClaim("Permission", Costanti.RegioniVisualizza) || !User.HasClaim("Permission", Costanti.RegioniUpsert)) return Forbid();

            var regionFromDb = GetRegione(codiceRegione);

            return View(regionFromDb);
        }

        public IActionResult EliminaRegione(int? id)
        {
            if (!User.HasClaim("Permission", Costanti.RegioniVisualizza) || !User.HasClaim("Permission", Costanti.RegioniDelete)) return Forbid();

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
        public IActionResult CancellaRegioni([FromBody] DeleteByIstatRequest request)
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
            if (!User.HasClaim("Permission", Costanti.RegioniVisualizza)) return Forbid();

            var regioni = _dbContext.TRegionis.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                regioni = regioni.Where(r => r.RegDescrizione.Contains(searchTerm));
            }

            var result = regioni.Select(r => new
            {
                codice = r.Codice,
                descrizione = r.Descrizione,
                istat = r.Istat,
                inizioValidita = r.InizioValidita,
                fineValidita = r.FineValidita,
            }).ToList();

            return Json(result);
        }

        [HttpGet]
        public TRegioni? GetRegione(int? codiceRegione)
        {
            if (!User.HasClaim("Permission", Costanti.RegioniVisualizza)) return null;
            return _dbContext.TRegionis.Find(codiceRegione);
        }

        public IActionResult EsportaRegioni(string tipo)
        {
            if (!User.HasClaim("Permission", Costanti.RegioniVisualizza)) return Forbid();

            var listaRegioni = _dbContext.TRegionis.AsNoTracking().ToList();

            var dati = listaRegioni.Select(r => new {
                Codice_Istat_Regione = r.RegIstat,
                Nome_Regione = r.RegDescrizione,
                Data_Inizio_Validita = r.RegInizioValidita.ToString("dd/MM/yyyy"),
                Data_Fine_Validita = r.RegFineValidita.ToString("dd/MM/yyyy")
            }).ToList();

            if ("xlsx".Equals(tipo, StringComparison.OrdinalIgnoreCase))
            {
                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("Regioni");

                ws.Cells["A1"].LoadFromCollection(dati, true);

                string[] customHeaders = { "Codice ISTAT Regione", "Nome Regione", " Data Inizio Validità", "Data Fine Validità"};
                for (int i = 0; i < customHeaders.Length; i++)
                {
                    ws.Cells[1, i + 1].Value = customHeaders[i];
                }

                using (var headerRange = ws.Cells[1, 1, 1, 4])
                {
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                ws.Cells.AutoFitColumns();

                return File(package.GetAsByteArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Regioni.xlsx");
            }

            if ("csv".Equals(tipo, StringComparison.OrdinalIgnoreCase))
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = ";" };

                using var ms = new MemoryStream();
                using var sw = new StreamWriter(ms, Encoding.UTF8);
                using var csv = new CsvWriter(sw, config);
                csv.WriteRecords(dati);
                sw.Flush();
                return File(ms.ToArray(), "text/csv", "Regioni.csv");
            }

            return BadRequest("Formato non supportato");
        }
    }
}
