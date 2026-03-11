using AllineamentoAnagrafiche.Data;
using AllineamentoAnagrafiche.DTOs;
using AllineamentoAnagrafiche.Handlers;
using AllineamentoAnagrafiche.Models;
using AllineamentoAnagrafiche.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AnagraficheContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("DefaultConnection")
    ));
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UpsertService<TRegioni, RegioneDto>>();
builder.Services.AddScoped<UpsertService<TProvince, ProvinciaDto>>();
builder.Services.AddScoped<UpsertService<TComuni, ComuneDto>>();
builder.Services.AddScoped<RemoveService<TRegioni, TProvince>>();
builder.Services.AddScoped<RemoveService<TProvince, TComuni>>();
builder.Services.AddScoped<RemoveService<TComuni, TComuni>>();
builder.Services.AddScoped<AutorizzazioniService>();
builder.Services.AddScoped<LogService>();
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "SmartAuth";
    options.DefaultChallengeScheme = "SmartAuth";
}).AddCookie("CookieAuthScheme", options =>
{
    options.Cookie.Name = "UserSession";
    options.LoginPath = "/Utenti/Login";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
    options.SlidingExpiration = true;
    options.AccessDeniedPath = "/Home/AccessoNegato";
}).AddScheme<AuthenticationSchemeOptions, CustomAuthHandler>("HeaderAuthScheme", null)
//prima controlla se è presente l'header, in caso di successo prova il login tramite handler se no con il cookie normale
.AddPolicyScheme("SmartAuth", "SmartAuth", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        if (context.Request.Headers.ContainsKey("Auth"))
        {
            return "HeaderAuthScheme";
        }
        return "CookieAuthScheme";
    };
}
);


var app = builder.Build();

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
