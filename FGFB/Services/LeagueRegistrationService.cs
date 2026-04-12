using FGFB.Data;
using FGFB.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using Stripe;
using Stripe.Checkout;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace FGFB.Services
{
    public class LeagueRegistrationService
    {
        private readonly ApplicationDbContext _context;
        private readonly StripeSettings _stripe;
        private readonly MailchimpTransactionalSettings _mailchimpTransactional;
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly EmailSettings _emailSettings;

        public LeagueRegistrationService(
            ApplicationDbContext context,
            IOptions<StripeSettings> stripe,
            IOptions<MailchimpTransactionalSettings> mailchimpTransactional,
            IHttpClientFactory httpClientFactory,
            IOptions<EmailSettings> emailOptions)
        {
            _context = context;
            _stripe = stripe.Value;
            _mailchimpTransactional = mailchimpTransactional.Value;
            _httpClientFactory = httpClientFactory;
            _emailSettings = emailOptions.Value;
        }

        public decimal CalculateFee(decimal amount)
        {
            return Math.Round((amount * 0.03m) + 0.80m, 2);
        }
        public async Task ProcessCompletedCheckoutSessionAsync(Session session)
        {
            if (session == null)
                return;

            if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
                return;

            var exists = await _context.LeagueRegistrations
                .AnyAsync(x => x.StripeSessionId == session.Id);

            if (exists)
                return;

            if (session.Metadata == null ||
                !session.Metadata.ContainsKey("leagueId") ||
                !session.Metadata.ContainsKey("email") ||
                !session.Metadata.ContainsKey("entryFee") ||
                !session.Metadata.ContainsKey("processingFee"))
            {
                throw new InvalidOperationException("Stripe session metadata is incomplete.");
            }

            var leagueId = long.Parse(session.Metadata["leagueId"]);
            var email = session.Metadata["email"].Trim();
            var entryFee = decimal.Parse(session.Metadata["entryFee"]);
            var fee = decimal.Parse(session.Metadata["processingFee"]);
            var total = entryFee + fee;

            var league = await _context.Leagues
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(l => l.LeagueId == leagueId);

            if (league == null)
                throw new InvalidOperationException("League not found.");

            if (!string.Equals(league.Status, LeagueStatus.Open, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("League is closed.");

            var registeredCount = await _context.LeagueRegistrations
                .CountAsync(r => r.LeagueId == leagueId);

            if (registeredCount >= league.TotalRosters)
                throw new InvalidOperationException("League is full.");

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

            var league = await _context.Leagues
                .IgnoreQueryFilters()
                .FirstAsync(l => l.LeagueId == registration.LeagueId);

            return (registration, league);
        }
        public async Task<(LeagueRegistration Registration, bool CreatedNew)> CreateFreeRegistrationAsync(long leagueId, string email)
        {
            if (leagueId <= 0)
                throw new ArgumentException("Invalid leagueId.", nameof(leagueId));

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required.", nameof(email));

            email = email.Trim();

            var league = await _context.Leagues
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(l => l.LeagueId == leagueId);

            if (league == null)
                throw new InvalidOperationException("League not found.");

            if (!string.Equals(league.Status, LeagueStatus.Open, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("League is closed.");

            var existing = await _context.LeagueRegistrations
                .FirstOrDefaultAsync(x => x.LeagueId == leagueId && x.Email == email);

            if (existing != null)
                return (existing, false);

            var registeredCount = await _context.LeagueRegistrations
                .CountAsync(r => r.LeagueId == leagueId);

            if (registeredCount >= league.TotalRosters)
                throw new InvalidOperationException("League is full.");

            var reg = new LeagueRegistration
            {
                LeagueId = leagueId,
                Email = email,
                LeagueLink = league.JoinLink,
                PaymentStatus = "Registered",
                EntryFee = 0m,
                ProcessingFee = 0m,
                TotalPaid = 0m,
                CreatedAt = DateTime.UtcNow
            };

            _context.LeagueRegistrations.Add(reg);
            await _context.SaveChangesAsync();

            await SendLeagueAccessEmailAsync(email, league, 0m, 0m, 0m);

            return (reg, true);
        }

        private async Task SendLeagueAccessEmailAsync(
            string toEmail,
            League league,
            decimal entryFee,
            decimal processingFee,
            decimal totalPaid)
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress("Fan Girl Football", _emailSettings.Username));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = "Your Fan Girl Football league access details";

            message.Body = new TextPart("html")
            {
                Text = $@"
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
                <a href='{league.JoinLink}' target='_blank'
                   style='display:inline-block;background:#e6dcff;color:#4b2bbd;padding:12px 20px;border-radius:999px;text-decoration:none;font-weight:700;'>
                   Access Your League
                </a>
            </p>

            <p>If the button does not work, copy and paste this link:</p>
            <p>{league.JoinLink}</p>
        "
            };

            using var client = new SmtpClient();

            await client.ConnectAsync(
                _emailSettings.Host,
                _emailSettings.Port,
                SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(
                _emailSettings.Username,
                _emailSettings.Password);

            await client.SendAsync(message);

            await client.DisconnectAsync(true);
        }
        private async Task SendLeagueAccessEmailAsyncMailChimp(
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

            var cleanEmail = toEmail?.Trim();

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
                email = cleanEmail,
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