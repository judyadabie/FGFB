using FGFB.Data;
using FGFB.Models;
using FGFB.Services;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ContentfulService>();

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.None;
});
builder.Services.Configure<MailchimpSettings>(
    builder.Configuration.GetSection("Mailchimp"));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHttpClient();
builder.Services.Configure<StripeSettings>(
    builder.Configuration.GetSection("Stripe"));
builder.Services.AddScoped<LeagueRegistrationService>();
builder.Services.Configure<MailchimpTransactionalSettings>(
    builder.Configuration.GetSection("MailchimpTransactional"));
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

var app = builder.Build();

// Error handling / security
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Error");
//    app.UseHsts();
//}
//else
//{
//    app.UseExceptionHandler("/Error");
//}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCookiePolicy();

app.UseRouting();

app.UseAuthorization();

// Rewrites / redirects
app.UseRewriter(new RewriteOptions()
    .AddRedirect(@"^/2024/06/20/100-taylor-swift-themed-fantasy-football-team-names/$", "/Blog/taylor-swift-team-names", StatusCodes.Status301MovedPermanently)
    .AddRedirect(@"^/2024/08/15/dfs-101-a-beginners-guide-to-daily-fantasy-sports/$", "/Blog/beginners-guide-to-daily-fantasy-sports", StatusCodes.Status301MovedPermanently)
    .AddRedirectToHttps());

app.MapGet("/2024/06/20/100-taylor-swift-themed-fantasy-football-team-names/", () =>
    Results.Redirect("/Blog/taylor-swift-team-names", true));

app.MapGet("/2024/08/15/dfs-101-a-beginners-guide-to-daily-fantasy-sports/", () =>
    Results.Redirect("/Blog/beginners-guide-to-daily-fantasy-sports", true));

app.MapGet("/fantasy-football-team-names/", () =>
    Results.Redirect("/Blog/taylor-swift-team-names", true));

// Register Razor Pages before controller routes so /Articles (page) is matched first
app.MapRazorPages();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");



//app.UseStatusCodePagesWithRedirects("/Error");

app.Run();