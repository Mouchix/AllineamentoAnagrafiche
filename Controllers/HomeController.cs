using AllineamentoAnagrafiche.Data;
using AllineamentoAnagrafiche.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace AllineamentoAnagrafiche.Controllers
{ 
    public class HomeController : Controller
    {
        private readonly AnagraficheContext _db;

        public HomeController(AnagraficheContext db)
        {
            this._db = db;
        }

        public IActionResult Index()
        {
            IEnumerable<Regione> regioni = _db.Regioni.ToList();
            return View(regioni);
        }

        public IActionResult AccessoNegato()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public IActionResult GetProvince(int codiceRegione)
        {
            var province = _db.Province
                .Where(p => p.ProRegCodice == codiceRegione).ToList();
            return Json(province);
        }

        [HttpGet]
        public IActionResult GetComuni(int codiceProvincia)
        {
            var comuni = _db.Comuni
                .Where(c => c.ComProCodice == codiceProvincia).ToList();
            return Json(comuni);
        }
    }
}
