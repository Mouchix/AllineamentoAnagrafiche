using AllineamentoAnagrafiche.Data;
using AllineamentoAnagrafiche.Models;
using System.ClientModel.Primitives;

namespace AllineamentoAnagrafiche.Services
{
    public class LogService
    {
        private readonly AnagraficheContext _dbContext;

        public LogService(AnagraficheContext context)
        {
            this._dbContext = context;
        }

        public void RegistraLog(string metodo, string request, string response, long user = Costanti.SystemUserId)
        {
            TTipoAutorizzazioni? tipoAutorizzazione = _dbContext.TTipoAutorizzazionis.FirstOrDefault(t => t.NomeMetodo.Equals(metodo));
            if(tipoAutorizzazione != null)
            {
                TLogMessaggi messaggio = new()
                {
                    LogUser = user,
                    LogMetodo = tipoAutorizzazione.TipoAutorizzazioneCodice,
                    LogDataOra = DateTime.Now,
                    LogMessaggioRequest = request,
                    LogMessaggioResponse = response
                };
                this._dbContext.TLogMessaggis.Add(messaggio);
            }          
        }
    }
}
