using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NotikaIdentityEmail.Context;
using NotikaIdentityEmail.Entities;
using NotikaIdentityEmail.Models.IdentityModels;
using NotikaIdentityEmail.Models.JwtModels;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
builder.Services.AddDbContext<EmailContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddIdentity<AppUser,IdentityRole>().AddEntityFrameworkStores<EmailContext>().AddErrorDescriber<CustomIdentityValidator>
    ().AddTokenProvider<DataProtectorTokenProvider<AppUser>>(TokenOptions.DefaultProvider);
builder.Services.Configure<JwtSettingsModel>(builder.Configuration.GetSection("JwtSettingsKey"));
//Add Jwt + Cookie Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme=CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

}).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>

{
    options.LoginPath = "/Login/UserLogin";
    options.AccessDeniedPath = "/Error/AccessDenied";
})

.AddJwtBearer(opt =>
{
    var jwtSettings = builder.Configuration.GetSection("JwtSettingsKey").Get<JwtSettingsModel>()
        ?? throw new InvalidOperationException("JWT settings are not configured.");
    if (string.IsNullOrWhiteSpace(jwtSettings.Key))
    {
        throw new InvalidOperationException("JWT signing key is not configured. Set JwtSettingsKey:Key using User Secrets or an environment variable.");
    }
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer=true,    
        ValidateAudience=true,
        ValidateLifetime=true,
        ValidateIssuerSigningKey=true,
        ValidIssuer=jwtSettings.Issuer,
        ValidAudience=jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
    };
})
//Google Authentication
.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
{
    var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
    var googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");

    options.ClientId = string.IsNullOrWhiteSpace(googleClientId) ? "your-google-client-id" : googleClientId;
    options.ClientSecret = string.IsNullOrWhiteSpace(googleClientSecret) ? "your-google-client-secret" : googleClientSecret;
});

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();
app.UseStatusCodePagesWithReExecute("/Error/{0}");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
