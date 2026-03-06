using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AllineamentoAnagrafiche.Services;

namespace AllineamentoAnagrafiche.Handlers
{
    public class CustomAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly AuthService _authService;

        public CustomAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            AuthService authService) : base(options, logger, encoder)
        {
            _authService = authService;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        { 
            if (!Request.Headers.TryGetValue("Auth", out Microsoft.Extensions.Primitives.StringValues value))
            {
                return AuthenticateResult.NoResult();
            }

            string authHeader = value;

            var utente = _authService.VerificaAutenticazioneHeader(authHeader);

            if (utente == null)
            {
                return AuthenticateResult.Fail("Credenziali Header non valide");
            }

            
            var claims = new[] {
                new Claim(ClaimTypes.Name, utente.Username),
                new Claim("UserCodice", utente.UserCodice.ToString())
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
    }
}