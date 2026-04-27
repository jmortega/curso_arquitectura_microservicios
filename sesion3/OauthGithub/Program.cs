using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GitHub OAuth API",
        Version = "v1",
        Description = "API con autenticación OAuth de GitHub"
    });
});

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme       = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = "GitHub";
    })
    .AddCookie()
    .AddOAuth("GitHub", options =>
    {
        options.ClientId     = builder.Configuration["GitHub:ClientId"]!;
        options.ClientSecret = builder.Configuration["GitHub:ClientSecret"]!;

        options.AuthorizationEndpoint   = "https://github.com/login/oauth/authorize";
        options.TokenEndpoint           = "https://github.com/login/oauth/access_token";
        options.UserInformationEndpoint = "https://api.github.com/user";

        options.CallbackPath = "/signin-github";

        options.Scope.Add("read:user");
        options.Scope.Add("user:email");
        options.SaveTokens = true;

        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name,           "login");
        options.ClaimActions.MapJsonKey(ClaimTypes.Email,          "email");
        options.ClaimActions.MapJsonKey("urn:github:avatar",       "avatar_url");
        options.ClaimActions.MapJsonKey("urn:github:url",          "html_url");

        options.Events = new OAuthEvents
        {
            OnCreatingTicket = async ctx =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, ctx.Options.UserInformationEndpoint);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ctx.AccessToken);

                var response = await ctx.Backchannel.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var user = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                ctx.RunClaimActions(user.RootElement);
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "GitHub OAuth API v1");
    c.RoutePrefix = "swagger";
    // Inyecta un botón personalizado de login en la UI de Swagger
    c.HeadContent = """
        <style>
            .github-login-btn {
                background-color: #24292e;
                color: white;
                border: none;
                padding: 8px 16px;
                border-radius: 4px;
                cursor: pointer;
                font-size: 14px;
                margin: 10px 0;
            }
            .github-login-btn:hover { background-color: #3a3f44; }
            .auth-status { padding: 8px; font-size: 13px; color: #555; }
        </style>
    """;
    c.InjectJavascript("/swagger-auth.js");
});

app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();

// ── Endpoints ──────────────────────────────────────────────────────────────

app.MapGet("/", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription();

// Comprueba si el usuario está autenticado (lo usa el JS de Swagger)
app.MapGet("/auth/status", (HttpContext ctx) =>
{
    if (ctx.User.Identity?.IsAuthenticated == true)
    {
        return Results.Ok(new
        {
            authenticated = true,
            username = ctx.User.FindFirst(ClaimTypes.Name)?.Value,
            avatar   = ctx.User.FindFirst("urn:github:avatar")?.Value
        });
    }
    return Results.Ok(new { authenticated = false });
})
.WithTags("Auth")
.WithSummary("Estado de autenticación")
.WithDescription("Devuelve si el usuario está autenticado y sus datos básicos.");

// Inicia el flujo OAuth — abre esta URL en el navegador
app.MapGet("/auth/login", () =>
    Results.Challenge(
        new AuthenticationProperties { RedirectUri = "/swagger" },
        new[] { "GitHub" }
    ))
.WithTags("Auth")
.WithSummary("Login con GitHub")
.WithDescription("Redirige a GitHub para autenticarse. Ábrelo en el navegador, no desde Swagger UI.");

// Devuelve los datos del usuario autenticado
app.MapGet("/auth/perfil", (HttpContext ctx) =>
{
    var claims = ctx.User.Claims.Select(c => new { tipo = c.Type, valor = c.Value });
    return Results.Ok(claims);
})
.WithTags("Auth")
.WithSummary("Perfil del usuario")
.WithDescription("Devuelve los claims del usuario autenticado.")
.RequireAuthorization();

// Cierra la sesión
app.MapGet("/auth/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/swagger");
})
.WithTags("Auth")
.WithSummary("Cerrar sesión")
.WithDescription("Elimina la cookie de sesión y redirige a Swagger.");

// Repositorios del usuario autenticado
app.MapGet("/github/repos", async (HttpContext ctx, [FromServices] IHttpClientFactory factory) =>
{
    var token = await ctx.GetTokenAsync("access_token");

    var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("MiApp/1.0");

    var repos = await client.GetStringAsync("https://api.github.com/user/repos");
    return Results.Content(repos, "application/json");
})
.WithTags("GitHub")
.WithSummary("Repositorios")
.WithDescription("Lista los repositorios del usuario autenticado.")
.RequireAuthorization();

app.Run("http://localhost:5001");