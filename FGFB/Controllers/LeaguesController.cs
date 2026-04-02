using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FGFB.Data;
using FGFB.Models;
using FGFB.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace FGFB.Controllers
{
    public class LeaguesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly MailchimpSettings _mailchimpSettings;
        private readonly StripeSettings _stripeSettings;
        private readonly LeagueRegistrationService _registrationService;

        public LeaguesController(
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory,
            IOptions<MailchimpSettings> mailchimpOptions,
            IOptions<StripeSettings> stripeOptions,
            LeagueRegistrationService registrationService)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _mailchimpSettings = mailchimpOptions.Value;
            _stripeSettings = stripeOptions.Value;
            _registrationService = registrationService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(decimal? maxEntryFee, DateTime? draftDateFrom, bool bestBallOnly = false)
        {
            const int currentSeason = 2026;

            var query = _context.Leagues
                .Where(l => l.SeasonYear == currentSeason);

            if (maxEntryFee.HasValue)
            {
                query = query.Where(l => (l.EntryFee ?? 0) <= maxEntryFee.Value);
            }

            if (draftDateFrom.HasValue)
            {
                var startDate = draftDateFrom.Value.Date;
                query = query.Where(l => l.DraftDate.HasValue && l.DraftDate.Value.Date >= startDate);
            }

            if (bestBallOnly)
            {
                query = query.Where(l => l.BestBall);
            }

            var leagues = await query
                .OrderBy(l => l.DraftDate ?? DateTime.MaxValue)
                .ThenBy(l => l.EntryFee ?? 0)
                .ToListAsync();

            var vm = new LeaguesFilterViewModel
            {
                MaxEntryFee = maxEntryFee,
                DraftDateFrom = draftDateFrom,
                BestBallOnly = bestBallOnly,
                SeasonYear = currentSeason,
                Leagues = leagues
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Join(long id)
        {
            const int currentSeason = 2026;

            var league = await _context.Leagues
                .FirstOrDefaultAsync(l => l.LeagueId == id && l.SeasonYear == currentSeason);

            if (league == null)
                return NotFound();

            var vm = new LeagueJoinViewModel
            {
                LeagueId = league.LeagueId,
                LeagueType = league.LeagueType ?? "League",
                EntryFee = league.EntryFee,
                DraftDate = league.DraftDate
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(LeagueJoinViewModel vm)
        {
            const int currentSeason = 2026;

            var league = await _context.Leagues
                .FirstOrDefaultAsync(l => l.LeagueId == vm.LeagueId && l.SeasonYear == currentSeason);

            if (league == null)
                return NotFound();

            vm.LeagueType = league.LeagueType ?? "League";
            vm.EntryFee = league.EntryFee;
            vm.DraftDate = league.DraftDate;

            if (!ModelState.IsValid)
                return View(vm);

            var found = await EmailExistsInMailchimp(vm.Email!);

            if (!found)
            {
                vm.ErrorMessage = @"
<div class='join-error-inner'>
    <div class='join-error-title'>We couldn’t find your email</div>
    <div class='join-error-text'>
        Make sure you're using the same email you registered with Fan Girl Football.
    </div>
    <a href='https://fan-girl-football.mn.co/sign_up'
       target='_blank'
       class='join-error-cta'>
       Join Fan Girl Football – It’s Free →
    </a>
</div>";
                return View(vm);
            }

            return RedirectToAction(nameof(Payment), new { id = vm.LeagueId, email = vm.Email });
        }

        [HttpGet]
        public async Task<IActionResult> Payment(long id, string email)
        {
            const int currentSeason = 2026;

            var league = await _context.Leagues
                .FirstOrDefaultAsync(l => l.LeagueId == id && l.SeasonYear == currentSeason);

            if (league == null)
                return NotFound();

            var vm = new LeagueJoinViewModel
            {
                LeagueId = league.LeagueId,
                LeagueType = league.LeagueType ?? "League",
                EntryFee = league.EntryFee,
                DraftDate = league.DraftDate,
                Email = email,
                EmailValidated = true
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStripeCheckout(long leagueId, string email)
        {
            const int currentSeason = 2026;

            var league = await _context.Leagues
                .FirstOrDefaultAsync(l => l.LeagueId == leagueId && l.SeasonYear == currentSeason);

            if (league == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(email))
                return RedirectToAction(nameof(Payment), new { id = leagueId, email });

            if (!league.EntryFee.HasValue || league.EntryFee.Value <= 0)
                return BadRequest("This league does not have a valid entry fee.");

            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;

            var fee = _registrationService.CalculateFee(league.EntryFee.Value);
            var total = league.EntryFee.Value + fee;

            var successUrl = Url.Action(
                nameof(PaymentSuccess),
                "Leagues",
                new { session_id = "{CHECKOUT_SESSION_ID}" },
                Request.Scheme)!;

            var cancelUrl = Url.Action(
                nameof(Payment),
                "Leagues",
                new { id = leagueId, email },
                Request.Scheme)!;

            var options = new SessionCreateOptions
            {
                Mode = "payment",
                CustomerEmail = email,
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    { "leagueId", leagueId.ToString() },
                    { "email", email },
                    { "entryFee", league.EntryFee.Value.ToString("0.00") },
                    { "processingFee", fee.ToString("0.00") }
                },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            UnitAmount = (long)Math.Round(total * 100m, MidpointRounding.AwayFromZero),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"{league.LeagueType ?? "League"} Entry",
                                Description = $"Includes processing fee of {fee:C}"
                            }
                        }
                    }
                }
            };

            var sessionService = new SessionService();
            var session = await sessionService.CreateAsync(options);

            return Redirect(session.Url);
        }

        [HttpGet]
        public async Task<IActionResult> PaymentSuccess(string session_id)
        {
            if (string.IsNullOrWhiteSpace(session_id))
                return BadRequest();

            var result = await _registrationService.ProcessSessionAndReturnAsync(session_id);

            if (result == null)
            {
                return View(new LeaguePaymentSuccessViewModel
                {
                    PaymentStatus = "Processing"
                });
            }

            var registration = result.Value.Registration;
            var league = result.Value.League;

            var vm = new LeaguePaymentSuccessViewModel
            {
                LeagueType = league.LeagueType ?? "League",
                EntryFee = registration.EntryFee,
                DraftDate = league.DraftDate,
                Email = registration.Email,
                LeagueLink = registration.LeagueLink ?? "",
                PaymentStatus = registration.PaymentStatus
            };

            return View(vm);
        }

        private async Task<bool> EmailExistsInMailchimp(string email)
        {
            var client = _httpClientFactory.CreateClient();

            if (string.IsNullOrWhiteSpace(_mailchimpSettings.ApiKey) ||
                string.IsNullOrWhiteSpace(_mailchimpSettings.DataCenter))
            {
                throw new InvalidOperationException("Mailchimp settings are missing.");
            }

            var requestUrl =
                $"https://{_mailchimpSettings.DataCenter}.api.mailchimp.com/3.0/search-members?query={Uri.EscapeDataString(email)}";

            var authToken = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"anystring:{_mailchimpSettings.ApiKey}"));

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", authToken);

            using var response = await client.GetAsync(requestUrl);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Mailchimp lookup failed: {response.StatusCode} - {json}");

            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("exact_matches", out var exactMatches))
                return false;

            if (!exactMatches.TryGetProperty("members", out var members))
                return false;

            return members.ValueKind == JsonValueKind.Array && members.GetArrayLength() > 0;
        }
    }
}