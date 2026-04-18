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
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly MailchimpSettings _mailchimpSettings;
        private readonly StripeSettings _stripeSettings;
        private readonly EventRegistrationService _eventRegistrationService;
        private readonly IWebHostEnvironment _environment;

        public EventsController(
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory,
            IOptions<MailchimpSettings> mailchimpOptions,
            IOptions<StripeSettings> stripeOptions,
            EventRegistrationService eventRegistrationService,
            IWebHostEnvironment environment)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _mailchimpSettings = mailchimpOptions.Value;
            _stripeSettings = stripeOptions.Value;
            _eventRegistrationService = eventRegistrationService;
            _environment = environment;
        }

        [HttpGet]
        public IActionResult ChampionshipDraftWeekend()
        {
            return View("~/Views/Events/ChampionshipDraftWeekend.cshtml");
        }
        [HttpGet]
        public IActionResult ChampionshipDraftWeekendExplore()
        {
            return View("~/Views/Events/ChampionshipDraftWeekendExplore.cshtml");
        }
        [HttpGet]
        public IActionResult ChampionshipDraftWeekendSignup()
        {
            var vm = BuildSignupViewModel();
            return View("~/Views/Events/ChampionshipDraftWeekendSignup.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEventStripeCheckout(ChampionshipDraftWeekendSignupViewModel vm)
        {
            ApplyTicketPricing(vm);

            if (!ModelState.IsValid)
                return View("~/Views/Events/ChampionshipDraftWeekendSignup.cshtml", vm);

            var found = await EmailExistsInMailchimp(vm.Email.Trim());
            if (!found)
            {
                vm.ErrorMessage = "We couldn’t find that email in Fan Girl Football membership. Membership is free, and you must use the same email tied to your membership.";
                return View("~/Views/Events/ChampionshipDraftWeekendSignup.cshtml", vm);
            }

            var existingRegistration = await _context.EventRegistrations
                .FirstOrDefaultAsync(x =>
                    x.EventName == "Championship Draft Weekend" &&
                    x.Email == vm.Email.Trim() &&
                    x.PaymentStatus == "Completed");

            if (existingRegistration != null)
            {
                vm.ErrorMessage = "This email is already registered for Championship Draft Weekend.";
                return View("~/Views/Events/ChampionshipDraftWeekendSignup.cshtml", vm);
            }

            vm.IsMemberValidated = true;

            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;

            var processingFee = _eventRegistrationService.CalculateFee(vm.BaseTicketPrice + vm.LeagueFee);
            var total = vm.BaseTicketPrice + vm.LeagueFee + processingFee;

            var successUrl = $"{Request.Scheme}://{Request.Host}/Events/ChampionshipDraftWeekendSignupSuccess?session_id={{CHECKOUT_SESSION_ID}}";
            var cancelUrl = $"{Request.Scheme}://{Request.Host}/Events/ChampionshipDraftWeekendSignup";

            var options = new SessionCreateOptions
            {
                Mode = "payment",
                CustomerEmail = vm.Email,
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                AllowPromotionCodes = true,
                Metadata = new Dictionary<string, string>
                {
                    { "eventName", "Championship Draft Weekend" },
                    { "firstName", vm.FirstName },
                    { "lastName", vm.LastName },
                    { "email", vm.Email },
                    { "leagueLevel", vm.LeagueLevel },
                    { "leagueDisplayName", vm.LeagueDisplayName },
                    { "baseTicketPrice", vm.BaseTicketPrice.ToString("0.00") },
                    { "leagueFee", vm.LeagueFee.ToString("0.00") },
                    { "processingFee", processingFee.ToString("0.00") },
                    { "totalPaid", total.ToString("0.00") },
                    { "agreeToTerms", vm.AgreeToTerms.ToString() }
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
                                Name = "Championship Draft Weekend Registration",
                                Description = $"{vm.LeagueDisplayName} | Includes processing fee of {processingFee:C}"
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
        public async Task<IActionResult> ChampionshipDraftWeekendSignupSuccess(string session_id)
        {
            if (string.IsNullOrWhiteSpace(session_id))
                return BadRequest();

            var registration = await _eventRegistrationService.ProcessSessionAndReturnAsync(session_id);
            return View("~/Views/Events/ChampionshipDraftWeekendSignupSuccess.cshtml", registration);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> EventWebhook()
        {
            var json = await new StreamReader(Request.Body).ReadToEndAsync();

            try
            {
                Event stripeEvent;

                if (_environment.IsDevelopment())
                {
                    stripeEvent = EventUtility.ParseEvent(json);
                }
                else
                {
                    stripeEvent = EventUtility.ConstructEvent(
                        json,
                        Request.Headers["Stripe-Signature"],
                        _stripeSettings.WebhookSecret);
                }

                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Session;
                    if (session != null)
                    {
                        await _eventRegistrationService.ProcessCompletedCheckoutSessionAsync(session);
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest($"Webhook error: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ValidateMembershipEmail(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return Json(new
                    {
                        valid = false,
                        message = "Please enter your membership email."
                    });
                }

                var found = await EmailExistsInMailchimp(email.Trim());

                if (found)
                {
                    return Json(new
                    {
                        valid = true,
                        message = "Membership email verified."
                    });
                }

                return Json(new
                {
                    valid = false,
                    message = "We couldn’t find that email in Fan Girl Football membership. Membership is free. Sign up using the link above, then come back and use the same email here."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    valid = false,
                    message = $"Validation error: {ex.Message}"
                });
            }
        }

        private ChampionshipDraftWeekendSignupViewModel BuildSignupViewModel()
        {
            var vm = new ChampionshipDraftWeekendSignupViewModel
            {
                LeagueLevel = "0"
            };
            ApplyTicketPricing(vm);
            return vm;
        }

        private void ApplyTicketPricing(ChampionshipDraftWeekendSignupViewModel vm)
        {
            var today = DateTime.Today;
            const int eventYear = 2026;

            if (today <= new DateTime(eventYear, 5, 31))
            {
                vm.BaseTicketPrice = 67.77m;
                vm.PricingLabel = "Early Bird pricing through May 31";
            }
            else if (today <= new DateTime(eventYear, 6, 30))
            {
                vm.BaseTicketPrice = 77.77m;
                vm.PricingLabel = "June pricing through June 30";
            }
            else if (today <= new DateTime(eventYear, 7, 31))
            {
                vm.BaseTicketPrice = 87.77m;
                vm.PricingLabel = "July pricing through July 31";
            }
            else if (today <= new DateTime(eventYear, 8, 15))
            {
                vm.BaseTicketPrice = 97.77m;
                vm.PricingLabel = "Standard pricing through Aug 15";
            }
            else
            {
                vm.BaseTicketPrice = 107.77m;
                vm.PricingLabel = "Final pricing through event day";
            }
        }

        private async Task<bool> EmailExistsInMailchimp(string email)
        {
            var client = _httpClientFactory.CreateClient();

            var requestUrl = $"https://{_mailchimpSettings.DataCenter}.api.mailchimp.com/3.0/search-members?query={Uri.EscapeDataString(email)}";
            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"anystring:{_mailchimpSettings.ApiKey}"));

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