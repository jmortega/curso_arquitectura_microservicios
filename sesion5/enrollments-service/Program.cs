using Enrollments.API.Middleware;
using Enrollments.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerWithDocs();
builder.Services.AddEnrollmentsServices(builder.Configuration);
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Enrollments Service v1");
    c.RoutePrefix   = string.Empty;
    c.DocumentTitle = "Enrollments Service API";
    c.DisplayRequestDuration();
});

app.MapHealthChecks("/health");
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();
