using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using TagUpdater.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var configuration = builder.Configuration;
var services = builder.Services;

// Add authentication services
services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDistributedTokenCaches();

// Add required Azure API scopes
services.AddMicrosoftIdentityWebApiAuthentication(configuration, "AzureAd")
    .EnableTokenAcquisitionToCallDownstreamApi();

// Add Azure specific services
services.AddScoped<ResourceManagerService>();

// Add MVC services
services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

// Add Razor Pages
services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();