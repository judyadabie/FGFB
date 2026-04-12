using System.Text;
using System.Text.Json;
using FGFB.Data;
using FGFB.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace FGFB.Services
{
    public class EventRegistrationService
    {
        private readonly ApplicationDbContext _context;
        private readonly StripeSettings _stripeSettings;
        private readonly MailchimpTransactionalSettings _mailchimpTransactionalSettings;
        private readonly IHttpClientFactory _httpClientFactory;

        public EventRegistrationService(
            ApplicationDbContext context,
            IOptions<StripeSettings> stripeOptions,
            IOptions<MailchimpTransactionalSettings> mailchimpTransactionalOptions,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _stripeSettings = stripeOptions.Value;
            _mailchimpTransactionalSettings = mailchimpTransactionalOptions.Value;
            _httpClientFactory = httpClientFactory;
        }

        public decimal CalculateFee(decimal amount)
        {
            return Math.Round((amount * 0.03m) + 0.80m, 2);
        }

        public async Task ProcessCompletedCheckoutSessionAsync(Session session)
        {
            if (session == null || !string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
                return;

            var exists = await _context.EventRegistrations
                .AnyAsync(x => x.StripeSessionId == session.Id);

            if (exists)
                return;

            if (session.Metadata == null)
                throw new InvalidOperationException("Missing metadata in Stripe session.");

            var reg = new EventRegistration
            {
                EventName = session.Metadata["eventName"],
                FirstName = session.Metadata["firstName"],
                LastName = session.Metadata["lastName"],
                Email = session.Metadata["email"],
                LeagueLevel = session.Metadata["leagueLevel"],
                LeagueDisplayName = session.Metadata["leagueDisplayName"],
                BaseTicketPrice = decimal.Parse(session.Metadata["baseTicketPrice"]),
                LeagueFee = decimal.Parse(session.Metadata["leagueFee"]),
                ProcessingFee = decimal.Parse(session.Metadata["processingFee"]),
                TotalPaid = decimal.Parse(session.Metadata["totalPaid"]),
                AgreeToTerms = bool.Parse(session.Metadata["agreeToTerms"]),
                PaymentStatus = "Completed",
                StripeSessionId = session.Id,
                StripePaymentIntentId = session.PaymentIntentId,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.EventRegistrations.Add(reg);
            await _context.SaveChangesAsync();

            try
            {
                await SendEventConfirmationEmailAsync(reg);
            }
            catch
            {
            }
        }

        public async Task<EventRegistration?> ProcessSessionAndReturnAsync(string sessionId)
        {
            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;

            var service = new SessionService();
            var session = await service.GetAsync(sessionId);

            await ProcessCompletedCheckoutSessionAsync(session);

            return await _context.EventRegistrations
                .FirstOrDefaultAsync(x => x.StripeSessionId == sessionId);
        }

        private async Task SendEventConfirmationEmailAsync(EventRegistration registration)
        {
            if (string.IsNullOrWhiteSpace(_mailchimpTransactionalSettings.ApiKey) ||
                string.IsNullOrWhiteSpace(_mailchimpTransactionalSettings.FromEmail))
            {
                return;
            }

            var html = $@"
<h2>Your Championship Draft Weekend registration is confirmed</h2>
<p>Thanks for signing up!</p>
<ul>
    <li><strong>Name:</strong> {registration.FirstName} {registration.LastName}</li>
    <li><strong>Email:</strong> {registration.Email}</li>
    <li><strong>League:</strong> {registration.LeagueDisplayName}</li>
    <li><strong>Total Paid:</strong> {registration.TotalPaid:C}</li>
</ul>
<p>We’ll send more event details as the weekend gets closer.</p>";

            var payload = new
            {
                key = _mailchimpTransactionalSettings.ApiKey,
                message = new
                {
                    html,
                    subject = "Championship Draft Weekend Registration Confirmation",
                    from_email = _mailchimpTransactionalSettings.FromEmail,
                    from_name = _mailchimpTransactionalSettings.FromName,
                    to = new[]
                    {
                        new { email = registration.Email, type = "to" }
                    }
                }
            };

            var client = _httpClientFactory.CreateClient();
            var response = await client.PostAsync(
                "https://mandrillapp.com/api/1.0/messages/send",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

            response.EnsureSuccessStatusCode();
        }
    }
}