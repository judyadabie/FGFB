using System.Security.Claims;
using FGFB.Data;
using FGFB.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FGFB.Controllers
{
    public class CommishController : Controller
    {
        private readonly ApplicationDbContext _context;

        private static readonly HashSet<string> AllowedEmails =
            new(new[]
            {
                "judy@fangirlfootball.com",
                "stacy@fangirlfootball.com"
            }, StringComparer.OrdinalIgnoreCase);

        private const string CommishPassword = "F2398kfootball";

        public CommishController(ApplicationDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction(nameof(Index));
            }

            var vm = new CommishLoginViewModel
            {
                ReturnUrl = returnUrl
            };

            return View("~/Views/Commish/Login.cshtml", vm);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(CommishLoginViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Commish/Login.cshtml", vm);
            }

            var normalizedEmail = (vm.Email ?? string.Empty).Trim();

            if (!AllowedEmails.Contains(normalizedEmail) || vm.Password != CommishPassword)
            {
                vm.ErrorMessage = "Invalid email or password.";
                return View("~/Views/Commish/Login.cshtml", vm);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, normalizedEmail),
                new Claim(ClaimTypes.Email, normalizedEmail),
                new Claim(ClaimTypes.Role, "Commish")
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

            if (!string.IsNullOrWhiteSpace(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
            {
                return Redirect(vm.ReturnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var currentYear = DateTime.UtcNow.Year;

            var leagueRegistrations = await
                (from r in _context.LeagueRegistrations
                 join l in _context.Leagues on r.LeagueId equals l.LeagueId into leagueJoin
                 from l in leagueJoin.DefaultIfEmpty()
                 where l == null || l.SeasonYear == currentYear
                 orderby r.CreatedAt descending
                 select new LeagueRegistrationAdminRow
                 {
                     RegistrationId = r.RegistrationId,
                     LeagueId = r.LeagueId,
                     LeagueName = l != null ? l.Name : "(Unknown League)",
                     SeasonYear = l != null ? l.SeasonYear : null,
                     Email = r.Email,
                     EntryFee = r.EntryFee,
                     ProcessingFee = r.ProcessingFee,
                     TotalPaid = r.TotalPaid,
                     PaymentStatus = r.PaymentStatus,
                     LeagueLink = r.LeagueLink,
                     CreatedAt = r.CreatedAt
                 })
                .ToListAsync();

            var eventRegistrations = await _context.EventRegistrations
                .Where(e => e.CreatedAtUtc.Year == currentYear)
                .OrderByDescending(e => e.CreatedAtUtc)
                .Select(e => new EventRegistrationAdminRow
                {
                    EventRegistrationId = e.EventRegistrationId,
                    EventName = e.EventName,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    Email = e.Email,
                    LeagueLevel = e.LeagueLevel,
                    LeagueDisplayName = e.LeagueDisplayName,
                    BaseTicketPrice = e.BaseTicketPrice,
                    LeagueFee = e.LeagueFee,
                    ProcessingFee = e.ProcessingFee,
                    TotalPaid = e.TotalPaid,
                    PaymentStatus = e.PaymentStatus,
                    CreatedAtUtc = e.CreatedAtUtc
                })
                .ToListAsync();

            var vm = new CommishDashboardViewModel
            {
                LeagueRegistrations = leagueRegistrations,
                EventRegistrations = eventRegistrations
            };

            return View("~/Views/Commish/Index.cshtml", vm);
        }
    }
}