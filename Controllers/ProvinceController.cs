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
    public class ProvinceController : BaseController
    {
        private readonly UpsertService<TRegioni, RegioneDto> rUpsertService;
        private readonly UpsertService<TProvince, ProvinciaDto> pUpsertService;
        private readonly RemoveService<TProvince, TComuni> removeService;

        public ProvinceController(AnagraficheContext db, AuthService auth, UpsertService<TRegioni, RegioneDto> rService, UpsertService<TProvince, ProvinciaDto> pService, RemoveService<TProvince, TComuni> remove, LogService lService)
            : base(db, auth, lService)
        {
            this.rUpsertService = rService;
            this.pUpsertService = pService;
            this.removeService = remove;
        }

        public IActionResult IndexProvince()
        {
            if (!User.HasClaim("Permission", Costanti.ProvinceVisualizza)) return Forbid();
            ViewBag.PuoCreare = User.HasClaim("Permission", Costanti.ProvinceUpsert);
            ViewBag.PuoEliminare = User.HasClaim("Permission", Costanti.ProvinceDelete);
            return View();
        }

        public IActionResult CreaProvincia()
        {
            if (!User.HasClaim("Permission", Costanti.ProvinceVisualizza) || !User.HasClaim("Permission", Costanti.ProvinceUpsert)) return Forbid();
            return View();
        }

        public IActionResult ModificaProvincia(int? codiceProvincia, int? codiceRegione)
        {
            if (!User.HasClaim("Permission", Costanti.ProvinceVisualizza) || !User.HasClaim("Permission", Costanti.ProvinceUpsert)) return Forbid();

            TProvince? provinciaFromDb = GetProvincia(codiceProvincia);
            TRegioni? regioneFromDb = _dbContext.TRegionis.Find(codiceRegione);

            ProvinciaVM provinciaVM = new()
            {
                Provincia = provinciaFromDb,
                Regione = regioneFromDb,
            };
            return View(provinciaVM);
        }

        public IActionResult EliminaProvincia(int? id)
        {
            if (!User.HasClaim("Permission", Costanti.ProvinceVisualizza) || !User.HasClaim("Permission", Costanti.ProvinceDelete)) return Forbid();

            var provinciaFromDb = GetProvincia(id);

            return View(provinciaFromDb);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public IActionResult AggiornaProvince([FromBody]ProvinciaRootDto province)
        {
            string metodo = Costanti.ProvinceUpsert;
            string request = System.Text.Json.JsonSerializer.Serialize(province);

            AuthResponse result = CheckUser(metodo);

            if (result.Response.Equals("AA"))
            {
                using var transaction = _dbContext.Database.BeginTransaction();

                try
                {
                    if (!province.Province.IsNullOrEmpty())
                    {
                        foreach (ProvinciaDto provincia in province.Province)
                        {
                            int idRegioneReferenziata = this.rUpsertService.Upsert(provincia.Regione, r => r.RegIstat == provincia.Regione.CodiceISTAT);

                            this.pUpsertService.Upsert(provincia, p => p.ProIstat == provincia.CodiceISTAT, p => p.ProRegCodice = idRegioneReferenziata);
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
        public IActionResult CancellaProvince([FromBody]DeleteByIstatRequest request)
        {
            string metodo = Costanti.ProvinceDelete;

            AuthResponse result = CheckUser(metodo);

            if(result.Response.Equals("AA"))
            {
                result.Response = removeService.Remove(request.ForzaCancellazione, p => p.ProIstat == request.CodiceISTAT, pro => (c => c.ComProCodice == pro.Codice));
            }

            _logService.RegistraLog(metodo, request.CodiceISTAT + " - Eliminazione forzata: " + request.ForzaCancellazione, result.Response, result.Utente?.UserCodice ?? Costanti.SystemUserId);

            _dbContext.SaveChanges();
            return result.Response.Equals("AA") ? Ok() : BadRequest(result.Response);
        }

        [HttpGet]
        public IActionResult GetProvince(string searchTerm = "", int? regioneFiltro = null)
        {
            if (!User.HasClaim("Permission", Costanti.ProvinceVisualizza)) return Forbid();

            var province = _dbContext.TProvinces.AsQueryable();

            if (regioneFiltro != null)
            {
                province = province.Where(p => p.ProRegCodice == regioneFiltro);
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                province = province.Where(p => p.ProDescrizione.Contains(searchTerm));
            }

            var result = province.Select(p => new {
                codice = p.Codice,
                descrizione = p.Descrizione,
                istat = p.Istat,
                inizioValidita = p.InizioValidita,
                fineValidita = p.FineValidita,
                nomeRegione = p.ProRegCodiceNavigation.Descrizione,
                codiceRegione = p.ProRegCodiceNavigation.Codice
            }).ToList();

            return Json(result);
        }

        [HttpGet]
        public TProvince? GetProvincia(int? codiceProvincia)
        {
            if (!User.HasClaim("Permission", Costanti.ProvinceVisualizza)) return null;
            return _dbContext.TProvinces.Find(codiceProvincia);
        }

        public IActionResult EsportaProvince(int? codiceRegione)
        {
            if (!User.HasClaim("Permission", Costanti.ProvinceVisualizza)) return Forbid();

            var query = from provincia in _dbContext.TProvinces
                        join regione in _dbContext.TRegionis
                        on provincia.ProRegCodice equals regione.RegCodice
                        select new { provincia, regione };

            if (codiceRegione.HasValue && codiceRegione > 0)
            {
                query = query.Where(x => x.regione.RegCodice == codiceRegione);
            }

            var listaProvince = query.ToList();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Province");

            string[] headers = { "Codice ISTAT Provincia", "Nome Provincia", "Data Inizio Validità", "Data Fine Validità", "Codice ISTAT Regione", "Nome Regione" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cells[1, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
            }

            worksheet.Column(3).Style.Numberformat.Format = "dd/mm/yyyy";
            worksheet.Column(4).Style.Numberformat.Format = "dd/mm/yyyy";

            for (int i = 0; i < listaProvince.Count; i++)
            {
                worksheet.Cells[i + 2, 1].Value = listaProvince[i].provincia.ProIstat;
                worksheet.Cells[i + 2, 2].Value = listaProvince[i].provincia.ProDescrizione;
                worksheet.Cells[i + 2, 3].Value = listaProvince[i].provincia.ProInizioValidita.ToString("dd/MM/yyyy");
                worksheet.Cells[i + 2, 4].Value = listaProvince[i].provincia.ProFineValidita.ToString("dd/MM/yyyy");
                worksheet.Cells[i + 2, 5].Value = listaProvince[i].regione.RegIstat;
                worksheet.Cells[i + 2, 6].Value = listaProvince[i].regione.RegDescrizione;
            }

            worksheet.Cells.AutoFitColumns();

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            string fileName = $"Province.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
