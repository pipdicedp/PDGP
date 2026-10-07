using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using TradeLicence.Data;
using TradeLicence.Interfaces;
using TradeLicence.Repositories;
using TradeLicence.Services;
using WaterConnection.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Documents are now stored as bytes directly in the DB (Application_Documents.File_Path
// is varbinary(max)), so raise the default request body limit to comfortably fit
// scanned ID/ownership documents. Adjust if you expect larger files.
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 26_214_400; // 25 MB
});
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 26_214_400; // 25 MB
});

// Application services and repositories
builder.Services.AddScoped<ITradeLicenceRepository, TradeLicenceRepository>();
builder.Services.AddScoped<ITradeLicenceService, TradeLicenceService>();
builder.Services.AddScoped<IFileEncryptionService, FileEncryptionService>();
builder.Services.AddScoped<TradeLicence.Repositories.ApplicationRepository>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<WaterApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<ElectricityApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDistributedMemoryCache();   // required by session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddScoped<CaptchaService>();

// ---- Registration: PAN verification + mobile OTP ----
builder.Services.AddScoped<OtpService>();

if (builder.Configuration.GetValue<bool>("PanApi:UseMock", true))
    builder.Services.AddScoped<IPanVerificationService, MockPanVerificationService>();
else
    builder.Services.AddHttpClient<IPanVerificationService, ApiPanVerificationService>();

if (builder.Configuration.GetValue<bool>("Sms:UseConsole", true))
    builder.Services.AddScoped<ISmsSender, ConsoleSmsSender>();
else
    builder.Services.AddHttpClient<ISmsSender, HttpSmsSender>();

// ---- Registration: EMAIL OTP ----
// Always send real emails through SMTP
builder.Services.AddScoped<EmailOtpService>();
builder.Services.AddScoped<IOtpEmailSender, SmtpOtpEmailSender>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error/Index");
    app.UseHsts();
}

// Catches non-exception failures too — 404 Not Found, 403 Forbidden, etc.
app.UseStatusCodePagesWithReExecute("/Error/StatusCode/{0}");

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=PYGuidancehome}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();