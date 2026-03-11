using AllineamentoAnagrafiche.Data;
using AllineamentoAnagrafiche.DTOs;
using AllineamentoAnagrafiche.Models;
using AllineamentoAnagrafiche.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml;

namespace AllineamentoAnagrafiche.Controllers
{
    public class RegioniController : BaseController
    {
        private readonly UpsertService<TRegioni, RegioneDto> upsertService;
        private readonly RemoveService<TRegioni, TProvince> removeService;

        public RegioniController(AnagraficheContext db, AuthService auth, UpsertService<TRegioni, RegioneDto> service, RemoveService<TRegioni, TProvince> remove, LogService lService)
            :base(db, auth, lService)
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
            if (!User.HasClaim("Permission", Costanti.RegioniVisualizza)) return Forbid();

            var regioni = _dbContext.TRegionis.AsQueryable();

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
        public TRegioni? GetRegione(int? codiceRegione)
        {
            if (!User.HasClaim("Permission", Costanti.RegioniVisualizza)) return null;
            return _dbContext.TRegionis.Find(codiceRegione);
        }

        public IActionResult EsportaRegioni()
        {
            if (!User.HasClaim("Permission", Costanti.RegioniVisualizza)) return Forbid();

            var listaRegioni = _dbContext.TRegionis.ToList();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Regioni");

            string[] headers = { "Codice ISTAT regione", "Nome Regione", "Data Inizio Validità", "Data Fine Validità"};
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cells[1, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
            }

            worksheet.Column(3).Style.Numberformat.Format = "dd/mm/yyyy";
            worksheet.Column(4).Style.Numberformat.Format = "dd/mm/yyyy";

            for (int i = 0; i < listaRegioni.Count; i++)
            {
                worksheet.Cells[i + 2, 1].Value = listaRegioni[i].RegIstat;
                worksheet.Cells[i + 2, 2].Value = listaRegioni[i].RegDescrizione;
                worksheet.Cells[i + 2, 3].Value = listaRegioni[i].RegInizioValidita.ToString("dd/MM/yyyy");
                worksheet.Cells[i + 2, 4].Value = listaRegioni[i].RegFineValidita.ToString("dd/MM/yyyy");
            }

            worksheet.Cells.AutoFitColumns();

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            string fileName = $"Regioni.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
