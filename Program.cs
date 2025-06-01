using Ertaqy.Infrastructure;
using Ertaqy.Infrastructure.ClientAuthentication.Extensions;
using Ertaqy.Infrastructure.ClientAuthentication.Handler;
using Ertaqy.Infrastructure.ClientAuthentication.Settings;
using Ertaqy.Storage;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureAuthenticationSettings();
builder.Services.AddInfrastructureLayer(builder.Configuration);
builder.Services.AddInfrastructureAuthenticationLayer(builder.Configuration);
builder.Services.AddAuthentication(options =>
{

    // options.DefaultAuthenticateScheme = "_USR2"; // For authenticating cookies
    options.DefaultChallengeScheme = "jwt"; // For handling API/WebSocket challenges
    options.DefaultScheme = "_USR2"; // Set cookies as the default scheme


}).AddClientCookieAuthentication(
    sp => sp.GetRequiredService<ITicketStore>(),
    sp => sp.GetRequiredService<IOptions<OAuthConfig>>(),
    sp => sp.GetRequiredService<ILogger<Program>>()

   );
;
builder.Services.AddCors(options => options.AddPolicy("CorsPolicy",
              builder =>
              {
                  builder.AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowAnyOrigin();
              }));

// Configuration settings
//builder.Configuration
//    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
//    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

//// Register OAuthConfig with dependency injection
//builder.Services.Configure<OAuthConfig>(builder.Configuration.GetSection("OAuthConfig"));

//// Add Data Protection settings from configuration with default values
//var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"] ?? @"C:\keys";
//var dataProtectionAppName = builder.Configuration["DataProtection:ApplicationName"] ?? "ertaqy";

//builder.Services.AddDataProtection()
//    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
//    .SetApplicationName(dataProtectionAppName);

//// Read CookieSettings from configuration
//var cookieLoginPath = builder.Configuration["CookieSettings:LoginPath"] ?? "/account/login";
//var cookieName = builder.Configuration["CookieSettings:CookieName"] ?? "_USR2";


// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddTransient<ImageResizeMiddleware>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();

}


app.UseHttpsRedirection();

////  host/dl/* =>  wwwroot/_byhost/{host}/*
// File routing settings
const string multiDomainsUrlPath = "d/";
var singleDomainUrlPath = builder.Configuration["FilesRouting:urlpath"] ?? "files";
var filesLocalFolder = builder.Configuration["FilesRouting:localFolder"] ?? "_byhost";

// Prevent direct access to wwwroot/_byhost/{domain}/

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;

    // Block direct access to anything under /_byhost/
    if (path.StartsWith("/"+ filesLocalFolder))
    {
        context.Response.StatusCode = 403; // Forbidden
        await context.Response.WriteAsync("Access Denied.");
        return;
    }

    await next();
});

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;

    if (path.StartsWith("/"+ filesLocalFolder))
    {
        if (!context.User.Identity.IsAuthenticated)
        {
            context.Response.StatusCode = 401; // Unauthorized
            await context.Response.WriteAsync("Authentication Required.");
            return;
        }
    }

    await next();
});

// Ensure paths start with correct characters
if (!singleDomainUrlPath.StartsWith("/")) singleDomainUrlPath = "/" + singleDomainUrlPath;
if (!filesLocalFolder.StartsWith("\\")) filesLocalFolder = "\\" + filesLocalFolder;

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;

    // Define which folders should be protected
    bool requiresAuth = path.StartsWith("/d/secure/"); // Adjust path as needed
    bool requiresUserAuth = path.StartsWith("/files/secure/"); // Adjust path as needed

    if ((requiresAuth || requiresUserAuth) && !context.User.Identity.IsAuthenticated)
    {
        context.Response.StatusCode = 401; // Unauthorized
        await context.Response.WriteAsync("Authentication Required.");
        return;
    }

    await next();
});

// host/files/.. => rewrite to _byhost/host/...
// Middleware for routing files by host
app.UseMiddleware<RoutingFilesByHostMiddleware>(singleDomainUrlPath, multiDomainsUrlPath, filesLocalFolder);
app.UseImageResizeMiddleware();

// Rewrite rules
// storage.ertaqy.com/d/{domain}/* =>  wwwroot/_byhost/{domain}/*

//var rewriteOptions = new RewriteOptions()
//    .AddRewrite(@"^d/(.*)$", $"{filesLocalFolder}/$1", skipRemainingRules: true); // Adjust the regex pattern according to your needs
//var rewriteOptionsSecure = new RewriteOptions()
//    .AddRewrite(@"^d/secure/(.*)$", $"{filesLocalFolder}/$1", skipRemainingRules: true); // Adjust the regex pattern according to your needs
//  host/dl/* =>  wwwroot/_byhost/{host}/*

//app.UseRewriter(rewriteOptions);
//app.UseRewriter(rewriteOptionsSecure);

app.UseCors();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

