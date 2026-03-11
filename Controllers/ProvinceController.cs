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

        public IActionResult EsportaProvince(string tipo, int? codiceRegione)
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

            var dati = query.Select(p => new {
                Codice_Istat_Provincia = p.provincia.ProIstat,
                Nome_Provincia = p.provincia.ProDescrizione,
                Data_Inizio_Validita = p.provincia.ProInizioValidita.ToString("dd/MM/yyyy"),
                Data_Fine_Validita = p.provincia.ProFineValidita.ToString("dd/MM/yyyy"),
                Codice_Istat_Regione = p.regione.RegIstat,
                Nome_Regione = p.regione.RegDescrizione,
            }).ToList();

            if ("xlsx".Equals(tipo))
            {
                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("Province");

                ws.Cells["A1"].LoadFromCollection(dati, true);

                string[] customHeaders = { "Codice ISTAT Provincia", "Nome Provincia", " Data Inizio Validità", "Data Fine Validità", "Codice ISTAT Regione", "Nome Regione" };
                for (int i = 0; i < customHeaders.Length; i++)
                {
                    ws.Cells[1, i + 1].Value = customHeaders[i];
                }

                using (var headerRange = ws.Cells[1, 1, 1, 6])
                {
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                ws.Cells.AutoFitColumns();

                return File(package.GetAsByteArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "province.xlsx");
            }

            if ("csv".Equals(tipo, StringComparison.OrdinalIgnoreCase))
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = ";" };

                using var ms = new MemoryStream();
                using var sw = new StreamWriter(ms, Encoding.UTF8);
                using var csv = new CsvWriter(sw, config);
                csv.WriteRecords(dati);
                sw.Flush();
                return File(ms.ToArray(), "text/csv", "Province.csv");
            }

            return BadRequest("Formato non supportato");
        }
    }
}
