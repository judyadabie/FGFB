using FGFB.Models;
using FGFB.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;

namespace FGFB.Controllers
{
    [ApiController]
    [Route("stripe/webhook")]
    public class StripeWebhookController : ControllerBase
    {
        private readonly StripeSettings _stripeSettings;
        private readonly LeagueRegistrationService _registrationService;

        public StripeWebhookController(
            IOptions<StripeSettings> stripeOptions,
            LeagueRegistrationService registrationService)
        {
            _stripeSettings = stripeOptions.Value;
            _registrationService = registrationService;
        }

        [HttpPost]
        public async Task<IActionResult> Index()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var stripeSignature = Request.Headers["Stripe-Signature"];

            Event stripeEvent;

            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    json,
                    stripeSignature,
                    _stripeSettings.WebhookSecret);
            }
            catch
            {
                return BadRequest();
            }

            if (stripeEvent.Type == "checkout.session.completed")
            {
                if (stripeEvent.Data.Object is Stripe.Checkout.Session session)
                {
                    await _registrationService.ProcessSession(session.Id);
                }
            }

            return Ok();
        }
    }
}