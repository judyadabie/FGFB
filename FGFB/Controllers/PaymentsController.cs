using FGFB.Models;
using FGFB.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace FGFB.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly LeaguePaymentsOptions _paymentOptions;
        private readonly IConfiguration _configuration;
        private readonly ILeagueRegistrationService _registrationService;
        private readonly IMailchimpService _mailchimpService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            IOptions<LeaguePaymentsOptions> paymentOptions,
            IConfiguration configuration,
            ILeagueRegistrationService registrationService,
            IMailchimpService mailchimpService,
            ILogger<PaymentsController> logger)
        {
            _paymentOptions = paymentOptions.Value;
            _configuration = configuration;
            _registrationService = registrationService;
            _mailchimpService = mailchimpService;
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCheckoutSession(LeagueSignupViewModel model)
        {
            if (!ModelState.IsValid)
            {
                CalculateFees(model);
                return View("~/Views/Leagues/Signup.cshtml", model);
            }

            CalculateFees(model);

            var sessionOptions = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = _paymentOptions.SuccessUrl,
                CancelUrl = _paymentOptions.CancelUrl,
                CustomerEmail = model.Email,
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            UnitAmount = (long)(model.BaseFee * 100),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = model.LeagueName + " Entry Fee"
                            }
                        }
                    },
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            UnitAmount = (long)(model.ConvenienceFee * 100),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Convenience Fee"
                            }
                        }
                    }
                },
                Metadata = new Dictionary<string, string>
                {
                    { "FirstName", model.FirstName },
                    { "LastName", model.LastName },
                    { "Email", model.Email },
                    { "Phone", model.Phone ?? "" },
                    { "LeagueName", model.LeagueName }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(sessionOptions);

            await _registrationService.CreatePendingRegistrationAsync(model, session.Id);

            return Redirect(session.Url);
        }

        [HttpGet]
        public async Task<IActionResult> Success(string sessionId)
        {
            [HttpGet]
            public async Task<IActionResult> Success(string sessionId)
            {
                if (string.IsNullOrWhiteSpace(sessionId))
                    return RedirectToAction("Index", "Leagues");

                var registration = await _registrationService.GetBySessionIdAsync(sessionId);

                if (registration == null)
                    return RedirectToAction("Index", "Leagues");

                var viewModel = new LeaguePaymentSuccessViewModel
                {
                    FirstName = registration.FirstName,
                    LastName = registration.LastName,
                    Email = registration.Email,
                    LeagueName = registration.LeagueName,
                    BaseFee = registration.BaseFee,
                    ConvenienceFee = registration.ConvenienceFee,
                    TotalFee = registration.TotalFee,
                    PaidUtc = registration.PaidUtc,
                    LeagueUrl = _configuration["LeagueLinks:MainLeagueUrl"] ?? ""
                };

                return View(viewModel);
            }
        }

        [HttpGet]
        public IActionResult Cancel()
        {
            return View();
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Webhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var webhookSecret = _configuration["Stripe:WebhookSecret"];

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    webhookSecret
                );

                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Session;
                    if (session != null)
                    {
                        await _registrationService.MarkPaidAsync(session.Id, session.PaymentIntentId ?? "");

                        var registration = await _registrationService.GetBySessionIdAsync(session.Id);
                        if (registration != null)
                        {
                            var leagueUrl = _configuration["LeagueLinks:MainLeagueUrl"] ?? "";
                            await _mailchimpService.SyncPaidLeagueMemberAsync(registration, leagueUrl);
                        }
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stripe webhook failed.");
                return BadRequest();
            }
        }

        private void CalculateFees(LeagueSignupViewModel model)
        {
            model.BaseFee = _paymentOptions.BaseLeagueFee;
            model.ConvenienceFee = Math.Round(model.BaseFee * (_paymentOptions.ConvenienceFeePercent / 100m), 2);
            model.TotalFee = model.BaseFee + model.ConvenienceFee;
        }
    }
}