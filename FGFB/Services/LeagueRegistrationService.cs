using System.Net.Http.Headers;
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
    public class LeagueRegistrationService
    {
        private readonly ApplicationDbContext _context;
        private readonly StripeSettings _stripe;
        private readonly MailchimpTransactionalSettings _mailchimpTransactional;
        private readonly IHttpClientFactory _httpClientFactory;

        public LeagueRegistrationService(
            ApplicationDbContext context,
            IOptions<StripeSettings> stripe,
            IOptions<MailchimpTransactionalSettings> mailchimpTransactional,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _stripe = stripe.Value;
            _mailchimpTransactional = mailchimpTransactional.Value;
            _httpClientFactory = httpClientFactory;
        }

        public decimal CalculateFee(decimal amount)
        {
            return Math.Round((amount * 0.03m) + 0.30m, 2);
        }
        public async Task ProcessCompletedCheckoutSessionAsync(Session session)
        {
            if (session == null) return;
            if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase)) return;

            var exists = await _context.LeagueRegistrations
                .AnyAsync(x => x.StripeSessionId == session.Id);

            if (exists) return;

            if (session.Metadata == null ||
                !session.Metadata.ContainsKey("leagueId") ||
                !session.Metadata.ContainsKey("email") ||
                !session.Metadata.ContainsKey("entryFee") ||
                !session.Metadata.ContainsKey("processingFee"))
            {
                throw new InvalidOperationException("Stripe session metadata is incomplete.");
            }

            var leagueId = long.Parse(session.Metadata["leagueId"]);
            var email = session.Metadata["email"];
            var entryFee = decimal.Parse(session.Metadata["entryFee"]);
            var fee = decimal.Parse(session.Metadata["processingFee"]);
            var total = entryFee + fee;

            var league = await _context.Leagues.FindAsync(leagueId);
            if (league == null)
                throw new InvalidOperationException("League not found.");

            var reg = new LeagueRegistration
            {
                LeagueId = leagueId,
                Email = email,
                LeagueLink = league.JoinLink,
                PaymentStatus = "Completed",
                StripeSessionId = session.Id,
                StripePaymentIntentId = session.PaymentIntentId,
                EntryFee = entryFee,
                ProcessingFee = fee,
                TotalPaid = total,
                CreatedAt = DateTime.UtcNow
            };

            _context.LeagueRegistrations.Add(reg);
            await _context.SaveChangesAsync();

            try
            {
                await SendLeagueAccessEmailAsync(email, league, entryFee, fee, total);
            }
            catch
            {
                // swallow email failure so registration stays saved
            }
        }
        public async Task ProcessSession(string sessionId)
        {
            StripeConfiguration.ApiKey = _stripe.SecretKey;

            var service = new SessionService();
            var session = await service.GetAsync(sessionId);

            await ProcessCompletedCheckoutSessionAsync(session);
        }
        public async Task<(LeagueRegistration Registration, League League)?> ProcessSessionAndReturnAsync(string sessionId)
        {
            await ProcessSession(sessionId);

            var registration = await _context.LeagueRegistrations
                .FirstOrDefaultAsync(r => r.StripeSessionId == sessionId);

            if (registration == null)
                return null;

            var league = await _context.Leagues.FirstAsync(l => l.LeagueId == registration.LeagueId);

            return (registration, league);
        }

        private async Task SendLeagueAccessEmailAsync(
            string toEmail,
            League league,
            decimal entryFee,
            decimal processingFee,
            decimal totalPaid)
        {
            if (string.IsNullOrWhiteSpace(_mailchimpTransactional.ApiKey) ||
                string.IsNullOrWhiteSpace(_mailchimpTransactional.FromEmail))
            {
                throw new InvalidOperationException("Mailchimp Transactional settings are missing.");
            }

            var subject = "Your Fan Girl Football league access details";

            var html = $@"
                <h2>Thanks for signing up!</h2>

                <p>Here is how to access the league:</p>

                <ul>
                    <li><strong>League Type:</strong> {league.LeagueType}</li>
                    <li><strong>Entry Fee:</strong> {entryFee:C}</li>
                    <li><strong>Processing Fee:</strong> {processingFee:C}</li>
                    <li><strong>Total Paid:</strong> {totalPaid:C}</li>
                    <li><strong>Draft Date:</strong> {(league.DraftDate.HasValue ? league.DraftDate.Value.ToString("MMMM dd, yyyy h:mm tt") : "TBD")}</li>
                </ul>

                <p>
                    <a href='{league.JoinLink}'
                       target='_blank'
                       style='display:inline-block;background:#e6dcff;color:#4b2bbd;padding:12px 20px;border-radius:999px;text-decoration:none;font-weight:700;'>
                       Access Your League
                    </a>
                </p>

                <p>If the button does not work, copy and paste this link into your browser:</p>
                <p>{league.JoinLink}</p>
            ";

            var payload = new
            {
                key = _mailchimpTransactional.ApiKey,
                message = new
                {
                    html,
                    subject,
                    from_email = _mailchimpTransactional.FromEmail,
                    from_name = _mailchimpTransactional.FromName,
                    to = new[]
                    {
                        new
                        {
                            email = toEmail,
                            type = "to"
                        }
                    }
                }
            };

            var client = _httpClientFactory.CreateClient();

            var response = await client.PostAsync(
                "https://mandrillapp.com/api/1.0/messages/send",
                new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"));

            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Mailchimp Transactional send failed: {response.StatusCode} - {body}");
            }
        }
    }
}