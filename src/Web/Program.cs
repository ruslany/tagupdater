using System.Web;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using TagUpdater.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var configuration = builder.Configuration;
var services = builder.Services;

// Add authentication services with explicit configuration
services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})

.AddMicrosoftIdentityWebApp(options =>
{
    configuration.GetSection("AzureAd").Bind(options);
    
    // Add the Azure management scope directly in the OpenIdConnect configuration
    options.Scope.Add("https://management.azure.com/user_impersonation");
    
    options.Events = new OpenIdConnectEvents
    {
        OnRedirectToIdentityProvider = context =>
        {
            // Check if we're requesting consent explicitly
            if (context.Properties.Items.ContainsKey(OpenIdConnectDefaults.RedirectUriForCodePropertiesKey))
            {
                // Add prompt=consent only when explicitly requested
                context.ProtocolMessage.Prompt = "consent";
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            context.Response.Redirect("/Home/Error?message=" + HttpUtility.UrlEncode(context.Exception.Message));
            context.HandleResponse();
            return Task.CompletedTask;
        },
        // Handle token validation errors
        OnRemoteFailure = context =>
        {
            Console.WriteLine($"Remote authentication failure: {context.Failure?.Message}");
            
            // Check if this is a consent error
            if (context.Failure?.Message?.Contains("AADSTS65001") == true || 
                context.Failure?.Message?.Contains("consent") == true)
            {
                // Redirect to a page that explains consent is required
                context.Response.Redirect("/Home/ConsentRequired");
                context.HandleResponse();
            }
            
            return Task.CompletedTask;
        },
        // This is the important part - handle post-authentication redirect
        OnTokenValidated = context =>
        {
            // If we have a redirect stored, use it
            if (context.Properties.RedirectUri != null && 
                !context.Properties.RedirectUri.EndsWith("/signin-oidc"))
            {
                // Keep the redirect URI
                Console.WriteLine($"Will redirect to: {context.Properties.RedirectUri}");
            }
            return Task.CompletedTask;
        }
    };
})
.EnableTokenAcquisitionToCallDownstreamApi()
.AddInMemoryTokenCaches();

// Configure token acquisition explicitly
services.Configure<MicrosoftIdentityOptions>(options =>
{
    options.ResponseType = "code";
});

// Add Azure specific services
services.AddScoped<ResourceManagerService>();

// Add MVC services
services.AddControllersWithViews(options =>
{
    // This ensures unauthorized requests redirect to login
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
})
.AddMicrosoftIdentityUI();

// Add Razor Pages
// services.AddRazorPages();

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