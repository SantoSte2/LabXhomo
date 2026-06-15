using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Template.Enums;
using Template.Services;

namespace Template.Web.Features.Login
{
    public partial class LoginController : Controller
    {
        private readonly TemplateDbContext _dbContext;

        public LoginController(TemplateDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        [AllowAnonymous]
        
        public virtual async Task<IActionResult> Login()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return View(new LoginViewModel());
            }

            /*
             * Il cookie può sopravvivere al riavvio dell'app,
             * mentre la Session InMemory viene svuotata.
             * La ricostruiamo quindi partendo dai claim.
             */
            if (!RipristinaSessioneDaiClaims())
            {
                await HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);

                HttpContext.Session.Clear();

                return View(new LoginViewModel());
            }

            if (User.IsInRole("Super"))
            {
                return RedirectToAction(
                    "Index",
                    "Super");
            }

            return RedirectToAction(
                "Index",
                "Dipendente");
        }

        [HttpGet]
        [AllowAnonymous]
        public virtual IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public virtual async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Login tramite email: più vicino a un gestionale reale.
            var emailNormalizzata = model.Email.Trim().ToLower();

            var dipendente = await _dbContext.Dipendenti
                .FirstOrDefaultAsync(d =>
                    d.Email.ToLower() == emailNormalizzata &&
                    d.Password == model.Password);

            if (dipendente == null)
            {
                model.ErrorMessage = "Email o password non corretti";
                return View(model);
            }

            var nominativo = $"{dipendente.Nome} {dipendente.Cognome}";

            var iniziali =
                $"{dipendente.Nome[0]}{dipendente.Cognome[0]}"
                .ToUpperInvariant();

            var claims = new List<Claim>
{
                new Claim(
                    ClaimTypes.NameIdentifier,
                    dipendente.Id.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    nominativo),

                new Claim(
                    ClaimTypes.Email,
                    dipendente.Email),

                new Claim(
                    ClaimTypes.Role,
                    dipendente.Ruolo.ToString()),

                new Claim(
                    "Matricola",
                    dipendente.Matricola),

                new Claim(
                    "Iniziali",
                    iniziali)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    AllowRefresh = true
                });

            ImpostaSessioneUtente(
                dipendente.Id,
                nominativo,
                dipendente.Matricola,
                iniziali,
                dipendente.Ruolo.ToString());

            if (dipendente.Ruolo == RuoloUtente.Super)
            {
                return RedirectToAction("Index", "Super");
            }

            return RedirectToAction("Index", "Dipendente");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction(
                nameof(Login));
        }

        private bool RipristinaSessioneDaiClaims()
        {
            var idClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var nominativo = User.FindFirstValue(
                ClaimTypes.Name);

            var matricola = User.FindFirstValue(
                "Matricola");

            var iniziali = User.FindFirstValue(
                "Iniziali");

            var ruolo = User.FindFirstValue(
                ClaimTypes.Role);

            if (!int.TryParse(idClaim, out var dipendenteId) ||
                string.IsNullOrWhiteSpace(nominativo) ||
                string.IsNullOrWhiteSpace(matricola) ||
                string.IsNullOrWhiteSpace(iniziali) ||
                string.IsNullOrWhiteSpace(ruolo))
            {
                return false;
            }

            ImpostaSessioneUtente(
                dipendenteId,
                nominativo,
                matricola,
                iniziali,
                ruolo);

            return true;
        }

        private void ImpostaSessioneUtente(
            int dipendenteId,
            string nominativo,
            string matricola,
            string iniziali,
            string ruolo)
        {
            HttpContext.Session.SetInt32(
                "DipendenteId",
                dipendenteId);

            HttpContext.Session.SetString(
                "Nominativo",
                nominativo);

            HttpContext.Session.SetString(
                "Matricola",
                matricola);

            HttpContext.Session.SetString(
                "Iniziali",
                iniziali);

            HttpContext.Session.SetString(
                "Ruolo",
                ruolo);
        }
    }
}