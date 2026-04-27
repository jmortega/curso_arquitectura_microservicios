using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var jwtKey     = builder.Configuration["Jwt:SecretKey"]!;
var jwtIssuer  = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "GitHub OAuth API", Version = "v1" });

    // Permite enviar el JWT desde Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Pega aquí el token obtenido en /auth/token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
        // Cookie solo para el flujo intermedio de OAuth
        options.DefaultSignInScheme       = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie("Cookies") // esquema temporal durante el callback de GitHub
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtIssuer,
            ValidAudience            = jwtAudience,
            IssuerSigningKey         = signingKey
        };

        // Leer el JWT también desde la cookie (para Swagger UI con navegador)
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                if (ctx.Request.Cookies.TryGetValue("jwt", out var cookieToken))
                    ctx.Token = cookieToken;
                return Task.CompletedTask;
            }
        };
    })
    .AddOAuth("GitHub", options =>
    {
        options.ClientId              = builder.Configuration["GitHub:ClientId"]!;
        options.ClientSecret          = builder.Configuration["GitHub:ClientSecret"]!;
        options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
        options.TokenEndpoint         = "https://github.com/login/oauth/access_token";
        options.UserInformationEndpoint = "https://api.github.com/user";
        options.CallbackPath          = "/signin-github";
        options.SignInScheme          = "Cookies"; // esquema temporal
        options.Scope.Add("read:user");
        options.Scope.Add("user:email");
        options.SaveTokens = true;

        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
        options.ClaimActions.MapJsonKey(ClaimTypes.Name,           "login");
        options.ClaimActions.MapJsonKey(ClaimTypes.Email,          "email");
        options.ClaimActions.MapJsonKey("urn:github:avatar",       "avatar_url");

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
            },

            // Tras el callback de GitHub, generamos el JWT y lo guardamos en cookie
            OnTicketReceived = ctx =>
            {
                var claims = ctx.Principal!.Claims.ToList();

                // Añadimos el access_token de GitHub como claim
                var githubToken = ctx.Properties!.GetTokenValue("access_token");
                if (githubToken is not null)
                    claims.Add(new Claim("github_token", githubToken));

                var jwt = new JwtSecurityToken(
                    issuer:   jwtIssuer,
                    audience: jwtAudience,
                    claims:   claims,
                    expires:  DateTime.UtcNow.AddHours(8),
                    signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(jwt);

                // Guardamos el JWT en una cookie httpOnly
                ctx.Response.Cookies.Append("jwt", tokenString, new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                    Expires  = DateTimeOffset.UtcNow.AddHours(8)
                });

                ctx.Response.Redirect("/swagger");
                ctx.HandleResponse(); // evita que continúe el flujo de cookie normal
                return Task.CompletedTask;
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
    c.InjectJavascript("/swagger-auth.js");
});

app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();

// ── Endpoints ───────────────────────────────────────────────────────────────

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.MapGet("/auth/login", () =>
    Results.Challenge(
        new AuthenticationProperties { RedirectUri = "/swagger" },
        new[] { "GitHub" }
    ))
.WithTags("Auth")
.WithSummary("Login con GitHub")
.ExcludeFromDescription();

// Devuelve el JWT en texto plano para copiarlo en Swagger UI
app.MapGet("/auth/token", (HttpContext ctx) =>
{
    if (!ctx.Request.Cookies.TryGetValue("jwt", out var token))
        return Results.Unauthorized();

    return Results.Ok(new { token });
})
.WithTags("Auth")
.WithSummary("Obtener JWT")
.WithDescription("Devuelve el JWT generado tras el login. Cópialo y úsalo en el botón Authorize de Swagger.");

app.MapGet("/auth/perfil", (HttpContext ctx) =>
{
    var claims = ctx.User.Claims.Select(c => new { tipo = c.Type, valor = c.Value });
    return Results.Ok(claims);
})
.WithTags("Auth")
.WithSummary("Perfil del usuario")
.RequireAuthorization();

app.MapGet("/auth/logout", (HttpContext ctx) =>
{
    ctx.Response.Cookies.Delete("jwt");
    return Results.Redirect("/swagger");
})
.WithTags("Auth")
.WithSummary("Cerrar sesión");

app.MapGet("/github/repos", async (HttpContext ctx, [FromServices] IHttpClientFactory factory) =>
{
    var githubToken = ctx.User.FindFirst("github_token")?.Value;
    if (string.IsNullOrEmpty(githubToken))
        return Results.Unauthorized();

    var client = factory.CreateClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("MiApp/1.0");

    var repos = await client.GetStringAsync("https://api.github.com/user/repos");
    return Results.Content(repos, "application/json");
})
.WithTags("GitHub")
.WithSummary("Repositorios")
.RequireAuthorization();

app.Run("http://localhost:5001");